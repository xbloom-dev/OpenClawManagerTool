using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace OpenClawManager.Services;

/// <summary>
/// Wrapper kolem Windows Pseudo Console API (ConPTY).
///
/// Umožňuje spustit interaktivní konzolový proces (např. openclaw tui) a:
/// - Přijímat jeho stdout včetně ANSI escape sekvencí (barvy, kurzor, atd.)
/// - Posílat mu stdin (klávesy od uživatele)
/// - Fixní velikost terminálu (žádný resize ve v0.3)
///
/// API reference:
/// https://learn.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session
/// </summary>
public class ConPtyProcess : IDisposable
{
    // Fixní velikost terminálu pro v0.3 — musí odpovídat xterm.js cols/rows v TerminalControl.xaml.cs
    public const short DefaultCols = 120;
    public const short DefaultRows = 35;

    private IntPtr _hPC = IntPtr.Zero;
    private IntPtr _hPipeIn = IntPtr.Zero;
    private IntPtr _hPipeOut = IntPtr.Zero;
    private SafeFileHandle? _inputWriteHandle;
    private SafeFileHandle? _outputReadHandle;
    private FileStream? _inputStream;
    private FileStream? _outputStream;
    private PROCESS_INFORMATION _processInfo;
    private CancellationTokenSource? _readCts;

    /// <summary>
    /// Vyvolá se asynchronně při příchodu výstupu z procesu.
    /// String je raw výstup včetně ANSI escape sekvencí.
    /// </summary>
    public event Action<string>? OutputReceived;

    /// <summary>
    /// Vyvolá se když proces skončil.
    /// </summary>
    public event Action? ProcessExited;

    public bool IsRunning => _processInfo.hProcess != IntPtr.Zero;

    /// <summary>
    /// Spustí příkaz v ConPTY relaci.
    /// </summary>
    /// <param name="commandLine">Plný command line (např. "powershell.exe -NoLogo -Command openclaw tui")</param>
    /// <param name="workingDirectory">Pracovní adresář (null = current)</param>
    public void Start(string commandLine, string? workingDirectory = null)
    {
        if (IsRunning)
            throw new InvalidOperationException("Process is already running.");

        // 1. Vytvořit pipes pro stdin a stdout
        IntPtr inputReadSide = IntPtr.Zero, inputWriteSide = IntPtr.Zero;
        IntPtr outputReadSide = IntPtr.Zero, outputWriteSide = IntPtr.Zero;

        if (!CreatePipe(out inputReadSide, out inputWriteSide, IntPtr.Zero, 0))
            throw new InvalidOperationException("CreatePipe (stdin) failed.");

        if (!CreatePipe(out outputReadSide, out outputWriteSide, IntPtr.Zero, 0))
            throw new InvalidOperationException("CreatePipe (stdout) failed.");

        _hPipeIn = inputWriteSide;       // C# → proces (zápis)
        _hPipeOut = outputReadSide;      // proces → C# (čtení)

        // 2. Vytvořit pseudo console
        var size = new COORD { X = DefaultCols, Y = DefaultRows };
        int hr = CreatePseudoConsole(size, inputReadSide, outputWriteSide, 0, out _hPC);
        if (hr != 0)
            throw new InvalidOperationException($"CreatePseudoConsole failed (HRESULT 0x{hr:X8}).");

        // 3. PROCESS_INFORMATION a STARTUPINFOEX
        var startupInfo = new STARTUPINFOEX();
        startupInfo.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();

        // 4. PROC_THREAD_ATTRIBUTE_LIST s PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE
        IntPtr attrListSize = IntPtr.Zero;
        InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attrListSize);

        startupInfo.lpAttributeList = Marshal.AllocHGlobal(attrListSize);
        if (!InitializeProcThreadAttributeList(startupInfo.lpAttributeList, 1, 0, ref attrListSize))
            throw new InvalidOperationException("InitializeProcThreadAttributeList failed.");

        if (!UpdateProcThreadAttribute(
                startupInfo.lpAttributeList,
                0,
                (IntPtr)PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                _hPC,
                (IntPtr)IntPtr.Size,
                IntPtr.Zero,
                IntPtr.Zero))
            throw new InvalidOperationException("UpdateProcThreadAttribute failed.");

        // 5. CreateProcess
        var success = CreateProcess(
            null,
            commandLine,
            IntPtr.Zero,
            IntPtr.Zero,
            false,
            EXTENDED_STARTUPINFO_PRESENT,
            IntPtr.Zero,
            workingDirectory,
            ref startupInfo,
            out _processInfo);

        if (!success)
        {
            int err = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"CreateProcess failed (Win32 error {err}).");
        }

        // 6. Zavřít side pipes které drží proces (nepotřebujeme je v C#)
        CloseHandle(inputReadSide);
        CloseHandle(outputWriteSide);

        // 7. Wrapnout pipes do streams
        _inputWriteHandle = new SafeFileHandle(_hPipeIn, ownsHandle: false);
        _outputReadHandle = new SafeFileHandle(_hPipeOut, ownsHandle: false);

        _inputStream = new FileStream(_inputWriteHandle, FileAccess.Write);
        _outputStream = new FileStream(_outputReadHandle, FileAccess.Read);

        // 8. Spustit background reader
        _readCts = new CancellationTokenSource();
        _ = Task.Run(() => ReadOutputLoop(_readCts.Token));

        // 9. Sledovat process exit
        _ = Task.Run(() =>
        {
            var proc = Process.GetProcessById((int)_processInfo.dwProcessId);
            proc.WaitForExit();
            ProcessExited?.Invoke();
        });
    }

    /// <summary>
    /// Pošle data do stdin procesu (např. stisknuté klávesy).
    /// </summary>
    public void WriteInput(string data)
    {
        if (_inputStream == null || !IsRunning) return;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            _inputStream.Write(bytes, 0, bytes.Length);
            _inputStream.Flush();
        }
        catch
        {
            // Pipe může být zavřená pokud proces skončil
        }
    }

    /// <summary>
    /// Čte výstup z procesu v nekonečné smyčce a posílá ho přes OutputReceived event.
    /// </summary>
    private async Task ReadOutputLoop(CancellationToken cancellationToken)
    {
        if (_outputStream == null) return;

        var buffer = new byte[4096];
        var decoder = Encoding.UTF8.GetDecoder();
        var charBuffer = new char[8192];

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int read;
                try
                {
                    read = await _outputStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                }
                catch
                {
                    break;
                }

                if (read == 0) break;

                int charsDecoded = decoder.GetChars(buffer, 0, read, charBuffer, 0);
                if (charsDecoded > 0)
                {
                    var text = new string(charBuffer, 0, charsDecoded);
                    OutputReceived?.Invoke(text);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        _readCts?.Cancel();

        if (_processInfo.hProcess != IntPtr.Zero)
        {
            try
            {
                // Zabít celý process tree (cmd.exe + node.exe a další děti).
                // TerminateProcess sám o sobě zabije jen parent — node.exe by zůstal
                // osiřelý a držel by ConPTY session, což blokuje nové TUI spojení.
                var pid = (int)_processInfo.dwProcessId;
                try
                {
                    using var parentProc = Process.GetProcessById(pid);
                    parentProc.Kill(entireProcessTree: true);
                    parentProc.WaitForExit(2000);
                }
                catch
                {
                    // Fallback na Win32 TerminateProcess pokud .NET API selže
                    TerminateProcess(_processInfo.hProcess, 0);
                }
            }
            catch { }

            CloseHandle(_processInfo.hProcess);
            CloseHandle(_processInfo.hThread);
            _processInfo = default;
        }

        if (_hPC != IntPtr.Zero)
        {
            ClosePseudoConsole(_hPC);
            _hPC = IntPtr.Zero;
        }

        _inputStream?.Dispose();
        _outputStream?.Dispose();
    }

    // ===================== Win32 P/Invoke =====================

    private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
    private const int PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD { public short X; public short Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(out IntPtr hReadPipe, out IntPtr hWritePipe, IntPtr lpPipeAttributes, uint nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateProcess(IntPtr hProcess, uint exitCode);

    [DllImport("kernel32.dll")]
    private static extern int CreatePseudoConsole(COORD size, IntPtr hInput, IntPtr hOutput, uint dwFlags, out IntPtr phPC);

    [DllImport("kernel32.dll")]
    private static extern int ClosePseudoConsole(IntPtr hPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, uint dwFlags, ref IntPtr lpSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr Attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcess(
        string? lpApplicationName,
        string lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        ref STARTUPINFOEX lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);
}
