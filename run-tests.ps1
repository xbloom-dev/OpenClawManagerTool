$ErrorActionPreference = 'Stop'
dotnet build .\OpenClawManager.csproj
dotnet run --project .\TokenService.Tests\TokenService.Tests.csproj
