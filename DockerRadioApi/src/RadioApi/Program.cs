using System.CommandLine;

var nameOption = new Option<string?>(
    aliases: new[] { "--name", "-n" },
    description: "Name to greet");

var rootCommand = new RootCommand("Sample C# command-line app running in Docker")
{
    nameOption
};

rootCommand.SetHandler((string? name) =>
{
    var target = string.IsNullOrWhiteSpace(name) ? "world" : name.Trim();
    Console.WriteLine($"Hello, {target}!");
    Console.WriteLine($"Running on .NET {Environment.Version}");
    Console.WriteLine($"OS: {Environment.OSVersion}");
}, nameOption);

return await rootCommand.InvokeAsync(args);
