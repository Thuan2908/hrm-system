using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hrm.Runner;

internal static class Program
{
    private static Process? _apiProcess;
    private static Process? _webProcess;
    private static readonly object _syncLock = new();
    private static bool _isStopping;

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("===================================================================");
        Console.WriteLine(" 🚀 Saigon Retail Management System - Local Development Runner");
        Console.WriteLine("===================================================================");

        var rootDir = Directory.GetCurrentDirectory();
        EnsureLocalConfiguration(rootDir);

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            StopAll();
        };

        AppDomain.CurrentDomain.ProcessExit += (_, _) => StopAll();

        try
        {
            Console.WriteLine("[Runner] Starting Backend API (https://localhost:7060)...");
            _apiProcess = StartProject(
                rootDir,
                "apps/api/Hrm.Api/Hrm.Api.csproj",
                "https",
                "API",
                ConsoleColor.Cyan);

            Console.WriteLine("[Runner] Starting Frontend Web (https://localhost:7100)...");
            _webProcess = StartProject(
                rootDir,
                "apps/web/Hrm.Web/Hrm.Web.csproj",
                "https",
                "WEB",
                ConsoleColor.Green);

            Console.WriteLine("-------------------------------------------------------------------");
            Console.WriteLine(" ✨ Both applications are launching:");
            Console.WriteLine("    👉 Frontend UI: https://localhost:7100");
            Console.WriteLine("    👉 Backend API: https://localhost:7060");
            Console.WriteLine("    👉 Swagger/OpenAPI: https://localhost:7060/openapi/v1.json");
            Console.WriteLine("    👉 Health: https://localhost:7060/health/ready");
            Console.WriteLine("    👉 Press [Ctrl+C] to stop both services.");
            Console.WriteLine("-------------------------------------------------------------------");

            var apiExitTask = _apiProcess.WaitForExitAsync();
            var webExitTask = _webProcess.WaitForExitAsync();

            var completedTask = await Task.WhenAny(apiExitTask, webExitTask);
            if (!_isStopping)
            {
                if (completedTask == apiExitTask)
                {
                    Console.WriteLine("[Runner] API exited with code: " + _apiProcess.ExitCode);
                }
                else
                {
                    Console.WriteLine("[Runner] Web exited with code: " + _webProcess.ExitCode);
                }

                StopAll();
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("[Runner Error] " + ex.Message);
            Console.ResetColor();
            StopAll();
            return 1;
        }
    }

    private static Process StartProject(
        string rootDir,
        string projectRelativePath,
        string launchProfile,
        string prefix,
        ConsoleColor color)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectRelativePath}\" --launch-profile {launchProfile}",
            WorkingDirectory = rootDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false
        };

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            lock (_syncLock)
            {
                Console.ForegroundColor = color;
                Console.Write($"[{prefix}] ");
                Console.ResetColor();
                Console.WriteLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            lock (_syncLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"[{prefix} ERR] {e.Data}");
                Console.ResetColor();
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start project: {projectRelativePath}");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static void StopAll()
    {
        lock (_syncLock)
        {
            if (_isStopping)
            {
                return;
            }

            _isStopping = true;
            Console.WriteLine("\n[Runner] Shutting down applications...");

            KillProcess(_apiProcess, "API");
            KillProcess(_webProcess, "WEB");

            Console.WriteLine("[Runner] All processes stopped. Goodbye!");
        }
    }

    private static void KillProcess(Process? process, string name)
    {
        if (process is null || process.HasExited)
        {
            return;
        }

        try
        {
            Console.WriteLine($"[Runner] Stopping {name}...");
            process.Kill(entireProcessTree: true);
            process.WaitForExit(3000);
        }
        catch
        {
            // Ignore errors when terminating child process
        }
    }

    private static void EnsureLocalConfiguration(string rootDir)
    {
        var localSettingsFile = Path.Combine(
            rootDir,
            "apps",
            "api",
            "Hrm.Api",
            "appsettings.Development.Local.json");

        if (File.Exists(localSettingsFile))
        {
            return;
        }

        var envFile = Path.Combine(rootDir, ".env");
        if (!File.Exists(envFile))
        {
            return;
        }

        string? conn = null;
        string? jwt = null;

        foreach (var line in File.ReadAllLines(envFile))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("ConnectionStrings__DefaultConnection=", StringComparison.OrdinalIgnoreCase))
            {
                conn = trimmed["ConnectionStrings__DefaultConnection=".Length..].Trim();
            }
            else if (trimmed.StartsWith("Jwt__SigningKey=", StringComparison.OrdinalIgnoreCase))
            {
                jwt = trimmed["Jwt__SigningKey=".Length..].Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(conn) || string.IsNullOrWhiteSpace(jwt))
        {
            return;
        }

        var rootNode = new JsonObject
        {
            ["ConnectionStrings"] = new JsonObject
            {
                ["DefaultConnection"] = conn
            },
            ["Jwt"] = new JsonObject
            {
                ["SigningKey"] = jwt
            }
        };

        var json = rootNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(localSettingsFile, json);
        Console.WriteLine("[Runner] Auto-synced appsettings.Development.Local.json from .env");
    }
}
