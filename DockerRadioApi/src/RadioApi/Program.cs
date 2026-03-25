using System.Diagnostics;
using System.Runtime.InteropServices;

Console.WriteLine("radio-api interactive mode");
Console.WriteLine("Commands: play [file], sample, help, exit");

var player = new AudioPlayer();

while (true)
{
    Console.Write("> ");

    var line = Console.ReadLine();
    if (line is null)
    {
        break;
    }

    var command = line.Trim();
    if (command.Length == 0)
    {
        continue;
    }

    if (string.Equals(command, "exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (string.Equals(command, "help", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("play [file]  Play a WAV file. Defaults to the packaged sample.");
        Console.WriteLine("sample       Show the packaged sample path.");
        Console.WriteLine("exit         Stop the container process.");
        continue;
    }

    if (string.Equals(command, "sample", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine(AudioPlayer.DefaultSamplePath);
        continue;
    }

    if (command.StartsWith("play", StringComparison.OrdinalIgnoreCase))
    {
        var file = command.Length > 4 ? command[4..].Trim() : null;
        var result = await player.PlayAsync(file);

        Console.WriteLine(result.Message);
        if (!string.IsNullOrWhiteSpace(result.File))
        {
            Console.WriteLine($"File: {result.File}");
        }

        continue;
    }

    Console.WriteLine("Unknown command. Use: play [file], sample, help, exit");
}

internal sealed class AudioPlayer
{
    internal const string DefaultSamplePath = "/app/samples/DaveRaindance.wav";
    private readonly SemaphoreSlim _playbackLock = new(1, 1);

    public async Task<PlayResult> PlayAsync(string? file)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new PlayResult(false, "Playback is only supported on Linux containers.", null, null);
        }

        var targetPath = ResolvePath(file);

        if (!File.Exists(targetPath))
        {
            return new PlayResult(false, $"File not found: {targetPath}", targetPath, null);
        }

        if (!await _playbackLock.WaitAsync(0))
        {
            return new PlayResult(false, "Another playback is already running.", targetPath, null);
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "aplay",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            startInfo.ArgumentList.Add(targetPath);

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                Console.WriteLine(stdout.Trim());
            }

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                Console.Error.WriteLine(stderr.Trim());
            }

            return process.ExitCode == 0
                ? new PlayResult(true, "Playback completed.", targetPath, process.ExitCode)
                : new PlayResult(false, $"aplay exited with code {process.ExitCode}.", targetPath, process.ExitCode);
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    private static string ResolvePath(string? file)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            return DefaultSamplePath;
        }

        return Path.IsPathRooted(file)
            ? file
            : Path.GetFullPath(file, "/app");
    }
}

internal sealed record PlayResult(bool Success, string Message, string? File, int? ExitCode);