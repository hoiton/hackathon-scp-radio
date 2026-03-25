using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;
using RadioBlazorApp.Models;

namespace RadioBlazorApp.Services;

public class MqttClientService : IDisposable
{
    private IMqttClient? _mqttClient;
    private string _brokerAddress = "localhost";
    private int _brokerPort = 1883;
    private string _requestTopic = "sislink/l008/sl008u04/slp001/read/dab/senderList";
    private string _responseTopic = "sislink/l008/sl008u04/slp001/status/dab/senderList";
    private string _playTopic = "sislink/l008/sl008u04/slp001/write/dab/play";
    private string _stopTopic = "sislink/l008/sl008u04/slp001/write/dab/stop";
    private string _volumeTopic = "sislink/l008/sl008u04/slp001/write/dab/volume";

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

    public async Task PlayStationAsync(RadioStation station, int volume = 50)
    {
        if (_mqttClient == null || !_mqttClient.IsConnected)
        {
            OnStatusChanged?.Invoke("Nicht verbunden! Bitte zuerst verbinden.");
            return;
        }

        try
        {
            var playCommand = new
            {
                station = new
                {
                    label = station.Label,
                    serviceId = station.ServiceId,
                    subChannelId = station.SubChannelId
                },
                volume = volume
            };

            var jsonPayload = JsonSerializer.Serialize(playCommand);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_playTopic)
                .WithPayload(jsonPayload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(message);
            OnStatusChanged?.Invoke($"🎵 Spiele Sender: {station.Label} (Lautstärke: {volume}%)");
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"❌ Fehler beim Abspielen: {ex.Message}");
        }
    }

    public async Task StopPlaybackAsync()
    {
        if (_mqttClient == null || !_mqttClient.IsConnected)
        {
            OnStatusChanged?.Invoke("Nicht verbunden! Bitte zuerst verbinden.");
            return;
        }

        try
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_stopTopic)
                .WithPayload("{}")
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(message);
            OnStatusChanged?.Invoke($"⏹️ Wiedergabe gestoppt");
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"❌ Fehler beim Stoppen: {ex.Message}");
        }
    }

    public async Task SetVolumeAsync(int volume)
    {
        if (_mqttClient == null || !_mqttClient.IsConnected)
        {
            return;
        }

        try
        {
            var volumeCommand = new { volume = volume };
            var jsonPayload = JsonSerializer.Serialize(volumeCommand);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_volumeTopic)
                .WithPayload(jsonPayload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(message);
            OnStatusChanged?.Invoke($"🔊 Lautstärke: {volume}%");
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"❌ Fehler beim Setzen der Lautstärke: {ex.Message}");
        }
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            OnStatusChanged?.Invoke($"📨 Nachricht empfangen von Topic: {e.ApplicationMessage.Topic}");

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

            if (response?.Stations != null && response.Stations.Count > 0)
            {
                OnStatusChanged?.Invoke($"🎵 {response.Stations.Count} Sender gefunden - übertrage zur UI...");

                // Event auslösen
                OnStationsReceived?.Invoke(response.Stations);

                OnStatusChanged?.Invoke($"✅ {response.Stations.Count} Sender erfolgreich übertragen!");
            }
            else
            {
                OnStatusChanged?.Invoke("⚠️ Keine Sender in der Antwort gefunden");
            }
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"❌ Fehler beim Parsen der Nachricht: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _mqttClient?.Dispose();
    }
}
