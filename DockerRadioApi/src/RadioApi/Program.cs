using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using RadioApi;

var mqttHost = "localhost";
var mqttPort = 1883;

for (var i = 0; i < args.Length; i++)
{
    if (string.Equals(args[i], "--mqtt-host", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
    {
        mqttHost = args[++i];
        continue;
    }

    if (string.Equals(args[i], "--mqtt-port", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
    {
        if (!int.TryParse(args[++i], out mqttPort))
        {
            Console.Error.WriteLine("Invalid value for --mqtt-port. Expected an integer.");
            return;
        }

        continue;
    }
}

Console.WriteLine("radio-api interactive mode");
Console.WriteLine("Commands: play [file], sample, services [file], help, exit");
Console.WriteLine($"MQTT broker: {mqttHost}:{mqttPort}");

var player = new AudioPlayer();

var mqttClient = new MqttClient(mqttHost, mqttPort);
await mqttClient.Start();

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
        Console.WriteLine("play [file]      Play a WAV file. Defaults to the packaged sample.");
        Console.WriteLine("sample           Show the packaged sample path.");
        Console.WriteLine("services [file]  Parse the DAB+ sender list file.");
        Console.WriteLine("exit             Stop the container process.");
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

    if (command.StartsWith("services", StringComparison.OrdinalIgnoreCase))
    {
        var file = command.Length > "services".Length ? command["services".Length..].Trim() : null;
        var path = string.IsNullOrWhiteSpace(file)
            ? ServiceListParser.DefaultServiceListPath
            : file;

        var result = await ServiceListParser.ParseFileAsync(path);

        if (!result.Success)
        {
            Console.WriteLine(result.Message);
            continue;
        }

        Console.WriteLine(result.Message);

        foreach (var group in result.Services.GroupBy(service => service.FrequencyMHz))
        {
            Console.WriteLine($"{group.Key.ToString("0.000", CultureInfo.InvariantCulture)} MHz");

            foreach (var service in group.OrderBy(service => service.Label, StringComparer.OrdinalIgnoreCase))
            {
                var started = service.Started ? "*" : "-";
                Console.WriteLine(
                    $"  [{service.SubChannelId,2}] {service.Label} | serviceId={service.ServiceId} strength={service.Strength} country={service.Country} version={service.Version} started={started}");
            }
        }

        continue;
    }

    Console.WriteLine("Unknown command. Use: play [file], sample, services [file], help, exit");
}

internal sealed class AudioPlayer
{
    internal const string DefaultSamplePath = "/app/samples/DaveRaindance.wav";
    private Process? _playbackProcess;
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

            _playbackProcess = new Process { StartInfo = startInfo };
            _playbackProcess.Start();

            var stdout = await _playbackProcess.StandardOutput.ReadToEndAsync();
            var stderr = await _playbackProcess.StandardError.ReadToEndAsync();
            await _playbackProcess.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                Console.WriteLine(stdout.Trim());
            }

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                Console.Error.WriteLine(stderr.Trim());
            }

            return _playbackProcess.ExitCode == 0
                ? new PlayResult(true, "Playback completed.", targetPath, _playbackProcess.ExitCode)
                : new PlayResult(false, $"aplay exited with code {_playbackProcess.ExitCode}.", targetPath, _playbackProcess.ExitCode);
        }
        finally
        {
            _playbackLock.Release();
            _playbackProcess?.Dispose();
        }
    }

    public void Stop()
    {
        _playbackProcess?.Kill();
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

internal static partial class ServiceListParser
{
    public const string DefaultServiceListPath = "/sys/bus/spi/devices/spi0.1/si468x_service_list";

    public static async Task<ServiceListParseResult> ParseFileAsync(string path)
    {
        var resolvedPath = Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(path, "/app");

        if (!File.Exists(resolvedPath))
        {
            return new ServiceListParseResult(false, $"Service list file not found: {resolvedPath}", []);
        }

        var lines = await File.ReadAllLinesAsync(resolvedPath);
        var services = new List<DabService>();

        foreach (var line in lines)
        {
            if (!TryParseServiceLine(line, out var service))
            {
                continue;
            }

            services.Add(service);
        }

        return services.Count == 0
            ? new ServiceListParseResult(false, $"No service entries found in {resolvedPath}.", [])
            : new ServiceListParseResult(true, $"Parsed {services.Count} services from {resolvedPath}.", services);
    }

    private static bool TryParseServiceLine(string line, out DabService service)
    {
        service = default!;

        var match = ServiceLineRegex().Match(line);
        if (!match.Success)
        {
            return false;
        }

        service = new DabService(
            double.Parse(match.Groups["frequency"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["serviceId"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["subChannelId"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["fic"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["strength"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["country"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["version"].Value, CultureInfo.InvariantCulture),
            string.Equals(match.Groups["started"].Value, "*", StringComparison.Ordinal),
            match.Groups["label"].Value.Trim());

        return true;
    }

    [GeneratedRegex(
        @"^\s*(?<frequency>\d+\.\d+)\s+(?<serviceId>\d+)\s+(?<subChannelId>\d+)\s+(?<fic>\d+)\s+(?<strength>\d+)\s+(?<country>\d+)\s+(?<version>\d+)\s+(?<started>[-*])\s+(?<label>.+?)\s*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ServiceLineRegex();
}

internal sealed record PlayResult(bool Success, string Message, string? File, int? ExitCode);

internal sealed record DabService(
    double FrequencyMHz,
    int ServiceId,
    int SubChannelId,
    int Fic,
    int Strength,
    int Country,
    int Version,
    bool Started,
    string Label);

internal sealed record ServiceListParseResult(bool Success, string Message, IReadOnlyList<DabService> Services);