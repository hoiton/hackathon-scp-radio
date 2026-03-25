using MQTTnet;
using MQTTnet.Server;
using System.Text;
using System.Text.Json;

namespace RadioBlazorApp.TestMqttBroker;

/// <summary>
/// Ein einfacher MQTT-Broker für Testzwecke
/// Dieser antwortet automatisch auf GET_STATIONS Kommandos mit einer Test-Senderliste
/// </summary>
public class TestMqttBroker
{
    private MqttServer? _mqttServer;

    public async Task StartAsync(int port = 1883)
    {
        var mqttFactory = new MqttFactory();

        var mqttServerOptions = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(port)
            .Build();

        _mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions);

        _mqttServer.ClientConnectedAsync += OnClientConnectedAsync;
        _mqttServer.InterceptingPublishAsync += OnMessageReceivedAsync;

        await _mqttServer.StartAsync();
        Console.WriteLine($"MQTT Test-Broker gestartet auf Port {port}");
        Console.WriteLine("Warte auf Verbindungen...");
    }

    private Task OnClientConnectedAsync(ClientConnectedEventArgs e)
    {
        Console.WriteLine($"Client verbunden: {e.ClientId}");
        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(InterceptingPublishEventArgs e)
    {
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
        Console.WriteLine($"Nachricht empfangen auf Topic '{e.ApplicationMessage.Topic}': {payload}");

        // Wenn GET_STATIONS empfangen wird, sende Test-Daten zurück
        if (payload == "GET_STATIONS" && e.ApplicationMessage.Topic == "radio/command")
        {
            await Task.Delay(500); // Simuliere Verarbeitungszeit

            var stations = new[]
            {
                new { Name = "Radio SRF 1", Frequency = "97.6 MHz", SignalStrength = 95, Genre = "Nachrichten" },
                new { Name = "Radio Energy Zürich", Frequency = "100.9 MHz", SignalStrength = 88, Genre = "Pop" },
                new { Name = "Radio 24", Frequency = "88.0 MHz", SignalStrength = 92, Genre = "Mix" },
                new { Name = "Radio Swiss Pop", Frequency = "101.7 MHz", SignalStrength = 78, Genre = "Pop" },
                new { Name = "Radio SRF 3", Frequency = "99.3 MHz", SignalStrength = 90, Genre = "Pop/Rock" },
                new { Name = "Radio Swiss Classic", Frequency = "106.5 MHz", SignalStrength = 85, Genre = "Klassik" },
                new { Name = "Radio Central", Frequency = "103.2 MHz", SignalStrength = 72, Genre = "Mix" },
                new { Name = "Radio Pilatus", Frequency = "105.5 MHz", SignalStrength = 68, Genre = "Mix" },
                new { Name = "Virgin Radio Switzerland", Frequency = "89.6 MHz", SignalStrength = 81, Genre = "Rock" },
                new { Name = "Radio SRF 2 Kultur", Frequency = "96.0 MHz", SignalStrength = 87, Genre = "Kultur" }
            };

            var json = JsonSerializer.Serialize(stations);

            var responseMessage = new MqttApplicationMessageBuilder()
                .WithTopic("radio/stations")
                .WithPayload(json)
                .Build();

            await _mqttServer!.InjectApplicationMessage(
                new InjectedMqttApplicationMessage(responseMessage)
                {
                    SenderClientId = "TestBroker"
                });

            Console.WriteLine($"Senderliste mit {stations.Length} Sendern verschickt");
        }
    }

    public async Task StopAsync()
    {
        if (_mqttServer != null)
        {
            await _mqttServer.StopAsync();
            Console.WriteLine("MQTT Test-Broker gestoppt");
        }
    }
}

// Standalone-Konsolenanwendung zum Starten des Test-Brokers
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== DAB+ Radio MQTT Test-Broker ===");
        Console.WriteLine();

        var broker = new TestMqttBroker();

        var port = 1883;
        if (args.Length > 0 && int.TryParse(args[0], out var customPort))
        {
            port = customPort;
        }

        await broker.StartAsync(port);

        Console.WriteLine();
        Console.WriteLine("Drücken Sie eine beliebige Taste zum Beenden...");
        Console.ReadKey();

        await broker.StopAsync();
    }
}
