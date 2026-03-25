using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;

var rootCommand = new RootCommand("Run the test sound script on Linux.");

rootCommand.SetHandler(async context =>
{
    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        Console.Error.WriteLine("This command only supports Linux.");
        context.ExitCode = 1;
        return;
    }

    var scriptPath = Path.Combine(Environment.CurrentDirectory, "aplay 165187__blaukreuz__global-village-hochdeutsch.wav");

    if (!File.Exists(scriptPath))
    {
        Console.Error.WriteLine($"Script not found: {scriptPath}");
        context.ExitCode = 1;
        return;
    }

    var startInfo = new ProcessStartInfo
    {
        FileName = "/bin/bash",
        ArgumentList = { scriptPath },
        WorkingDirectory = Environment.CurrentDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };

    using var process = new Process { StartInfo = startInfo };

    process.OutputDataReceived += (_, eventArgs) =>
    {
        if (eventArgs.Data is not null)
        {
            Console.WriteLine(eventArgs.Data);
        }
    };

    process.ErrorDataReceived += (_, eventArgs) =>
    {
        if (eventArgs.Data is not null)
        {
            Console.Error.WriteLine(eventArgs.Data);
        }
    };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    await process.WaitForExitAsync();
    context.ExitCode = process.ExitCode;
});

return await rootCommand.InvokeAsync(args);