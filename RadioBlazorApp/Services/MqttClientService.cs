using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;

namespace RadioBlazorApp.Services;

public class MqttClientService : IDisposable
{
    private IMqttClient? _mqttClient;
    private string _brokerAddress = "localhost";
    private int _brokerPort = 1883;
    private string _requestTopic = "sislink/l008/sl008u04/slp001/read/dab/senderList";
    private string _responseTopic = "sislink/l008/sl008u04/slp001/status/dab/senderList";

    public event Action<List<RadioStation>>? OnStationsReceived;
    public event Action<string>? OnStatusChanged;
    public bool IsConnected => _mqttClient?.IsConnected ?? false;

    public async Task ConnectAsync(string broker, int port, string requestTopic, string responseTopic)
    {
        _brokerAddress = broker;
        _brokerPort = port;
        _requestTopic = requestTopic;
        _responseTopic = responseTopic;

        try
        {
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_brokerAddress, _brokerPort)
                .WithCleanSession()
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

            await _mqttClient.ConnectAsync(options);
            OnStatusChanged?.Invoke($"Verbunden mit {_brokerAddress}:{_brokerPort}");

            // Subscribe zum Response-Topic
            await _mqttClient.SubscribeAsync(_responseTopic);
            OnStatusChanged?.Invoke($"Subscribed zu Topic: {_responseTopic}");
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"Fehler beim Verbinden: {ex.Message}");
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_mqttClient != null && _mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync();
            OnStatusChanged?.Invoke("Verbindung getrennt");
        }
    }

    public async Task SendGetStationsCommandAsync()
    {
        if (_mqttClient == null || !_mqttClient.IsConnected)
        {
            OnStatusChanged?.Invoke("Nicht verbunden! Bitte zuerst verbinden.");
            return;
        }

        try
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_requestTopic)
                .WithPayload("{}")
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(message);
            OnStatusChanged?.Invoke($"Kommando gesendet an Topic: {_requestTopic}");
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"Fehler beim Senden: {ex.Message}");
        }
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            OnStatusChanged?.Invoke($"Nachricht empfangen von Topic: {e.ApplicationMessage.Topic}");

            // Repariere JavaScript-Objektnotation zu gültigem JSON
            // Füge Anführungszeichen um Property-Namen hinzu
            var jsonPayload = System.Text.RegularExpressions.Regex.Replace(
                payload, 
                @"(\s*)(\w+)(\s*):", 
                "$1\"$2\"$3:");

            // Parse JSON mit stations-Array
            var response = JsonSerializer.Deserialize<StationsResponse>(jsonPayload, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (response?.Stations != null)
            {
                OnStationsReceived?.Invoke(response.Stations);
                OnStatusChanged?.Invoke($"{response.Stations.Count} Sender empfangen");
            }
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"Fehler beim Parsen der Nachricht: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _mqttClient?.Dispose();
    }
}

public class StationsResponse
{
    public List<RadioStation>? Stations { get; set; }
}

public class RadioStation
{
    public string Label { get; set; } = string.Empty;
    public double Mhz { get; set; }
    public int ServiceId { get; set; }
    public int SubChannelId { get; set; }
    public int Strength { get; set; }

    // Computed properties für Kompatibilität mit der UI
    public string? Name => Label;
    public string? Frequency => $"{Mhz:F3} MHz";
    public int SignalStrength => Strength;
    public string? Genre => "DAB+";
}
