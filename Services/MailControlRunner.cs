using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using OpenClawManager.Models;

namespace OpenClawManager.Services;

/// <summary>
/// Jediné místo, které mluví s backendem pošty a kalendáře na úrovni procesu.
/// Implementace: <see cref="ProcessMailControlRunner"/> (skutečný backend) a
/// <see cref="FakeMailControlRunner"/> (simulátor pro vývoj a testy).
/// </summary>
public interface IMailControlRunner
{
    /// <summary>
    /// Provede jeden příkaz ovládací vrstvy. Nikdy nevyhazuje výjimku (kromě zrušení tokenem
    /// se vrací chyba <see cref="MailControlError.Cancelled"/>).
    /// </summary>
    Task<MailControlResponse> RunAsync(
        string command,
        IReadOnlyDictionary<string, object?>? args,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

/// <summary>Zjištění umístění backendu (kořen instalace s control\control_run.py).</summary>
public static class MailControlPaths
{
    public const string FallbackRoot = @"E:\OpenClaw";

    private static string? _detected;

    public static string ControlRunRelativePath => Path.Combine("control", "control_run.py");

    public static string PythonRelativePath => Path.Combine("gcal", ".venv", "Scripts", "python.exe");

    public static bool IsValidRoot(string? root)
    {
        if (string.IsNullOrWhiteSpace(root))
            return false;

        try
        {
            return File.Exists(Path.Combine(root, ControlRunRelativePath));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Hledá <c>&lt;kořen&gt;\control\control_run.py</c>: předky složky aplikace, potom
    /// složku OpenClaw na pevných discích. Když nic nenajde, vrátí <see cref="FallbackRoot"/>.
    /// </summary>
    public static string DetectRoot()
    {
        if (_detected != null)
            return _detected;

        _detected = DetectRootCore();
        return _detected;
    }

    private static string DetectRootCore()
    {
        try
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (var i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
            {
                if (IsValidRoot(dir.FullName))
                    return dir.FullName;
            }
        }
        catch
        {
            // pokračovat dalšími kandidáty
        }

        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
                    continue;

                var candidate = Path.Combine(drive.RootDirectory.FullName, "OpenClaw");
                if (IsValidRoot(candidate))
                    return candidate;
            }
        }
        catch
        {
            // použít záložní hodnotu
        }

        return FallbackRoot;
    }
}

/// <summary>
/// Spouští <c>&lt;kořen&gt;\gcal\.venv\Scripts\python.exe &lt;kořen&gt;\control\control_run.py &lt;příkaz&gt;
/// [--args-json JSON]</c> a čte jeden JSON objekt UTF-8 ze stdout.
/// Argumenty se předávají přes ArgumentList (žádný spojený řetězec, žádný shell).
/// </summary>
public sealed class ProcessMailControlRunner : IMailControlRunner
{
    private readonly Func<string> _rootProvider;

    public ProcessMailControlRunner(string rootPath)
        : this(() => rootPath)
    {
    }

    public ProcessMailControlRunner(Func<string> rootProvider)
    {
        _rootProvider = rootProvider;
    }

    public async Task<MailControlResponse> RunAsync(
        string command,
        IReadOnlyDictionary<string, object?>? args,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var root = _rootProvider();
        var script = Path.Combine(root, MailControlPaths.ControlRunRelativePath);
        var python = Path.Combine(root, MailControlPaths.PythonRelativePath);

        if (!File.Exists(script))
            return MailControlResponse.Failure(MailControlError.RunnerUnavailable, script);
        if (!File.Exists(python))
            return MailControlResponse.Failure(MailControlError.RunnerUnavailable, python);

        var psi = BuildStartInfo(root, command, args);

        using var process = new Process { StartInfo = psi };
        try
        {
            process.Start();
            process.StandardInput.Close();
        }
        catch (Exception ex)
        {
            return MailControlResponse.Failure(MailControlError.StartFailed, ex.GetType().Name);
        }

        // stderr se čte, aby proces nezablokoval plný buffer; do UI ani logu se nepřenáší
        // (traceback může obsahovat cesty a text e-mailu).
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return cancellationToken.IsCancellationRequested
                ? MailControlResponse.Failure(MailControlError.Cancelled)
                : MailControlResponse.Failure(MailControlError.Timeout);
        }

        var stdout = await stdoutTask.ConfigureAwait(false);
        _ = await stderrTask.ConfigureAwait(false);
        return MailControlResponse.Parse(stdout);
    }

    /// <summary>
    /// Sestaví spuštění procesu. Argumenty jdou přes ArgumentList (žádný spojený řetězec ani shell),
    /// výstup se čte jako UTF-8 a Python dostane PYTHONUTF8=1.
    /// </summary>
    public static ProcessStartInfo BuildStartInfo(string root, string command, IReadOnlyDictionary<string, object?>? args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(root, MailControlPaths.PythonRelativePath),
            WorkingDirectory = root,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        psi.ArgumentList.Add(Path.Combine(root, MailControlPaths.ControlRunRelativePath));
        psi.ArgumentList.Add(command);
        if (args is { Count: > 0 })
        {
            psi.ArgumentList.Add("--args-json");
            psi.ArgumentList.Add(JsonSerializer.Serialize(args));
        }

        psi.Environment["PYTHONUTF8"] = "1";
        psi.Environment["PYTHONIOENCODING"] = "utf-8";
        return psi;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // proces už skončil
        }
    }
}
