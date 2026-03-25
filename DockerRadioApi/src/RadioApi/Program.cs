using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;

var rootCommand = new RootCommand("Play the packaged DaveRaindance sample on Linux.");

rootCommand.SetHandler(async context =>
{
    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        Console.Error.WriteLine("This command only supports Linux.");
        context.ExitCode = 1;
        return;
    }

    var samplePath = "/app/samples/DaveRaindance.wav";

    if (!File.Exists(samplePath))
    {
        Console.Error.WriteLine($"Sample file not found: {samplePath}");
        context.ExitCode = 1;
        return;
    }

    var startInfo = new ProcessStartInfo
    {
        FileName = "aplay",
        WorkingDirectory = Environment.CurrentDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };

    startInfo.ArgumentList.Add(samplePath);

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