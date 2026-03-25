using System.Buffers;
using System.Text;
using System.Text.Json;
using MQTTnet;

namespace RadioApi;

public sealed class MqttClient : IAsyncDisposable
{
    private readonly string MqttBrokerHost = "localhost";
    private readonly int MqttBrokerPort = 1883;
    private const string SisLinkWildcardTopic = "sislink/#";
    private const int InitialReconnectDelayMs = 1000;
    private const int MaxReconnectDelayMs = 60000;

    private readonly AudioPlayer _audioPlayer = new();

    private readonly MqttClientFactory mqttClientFactory;
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private IMqttClient mqttClient;
    private bool isIntentionalDisconnect;
    private bool isDisposed;
    private int reconnectAttempts;

    public MqttClient(string mqttBrokerHost, int mqttBrokerPort)
    {
        this.mqttClientFactory = new MqttClientFactory();
        MqttBrokerHost = mqttBrokerHost;
        MqttBrokerPort = mqttBrokerPort;
    }

    public async Task<bool> Start()
    {
        if (this.isDisposed)
        {
            Console.WriteLine($"{nameof(this.Start)}: Module is disposed, cannot start");
            return false;
        }

        await this.semaphore.WaitAsync();

        try
        {
            await this.CleanupClient();

            return await this.ConnectClient();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{nameof(this.Start)}: Error: {ex.Message}");
            return false;
        }
        finally
        {
            this.semaphore.Release();
            Console.WriteLine($"{nameof(this.Start)}: Done");
        }
    }

    private async Task<bool> ConnectClient()
    {
        this.mqttClient = this.mqttClientFactory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(MqttBrokerHost, MqttBrokerPort)
            .WithClientId("DabRadioModule")
            .WithCleanSession(true)
            .WithTimeout(TimeSpan.FromSeconds(10))
            .Build();

        this.mqttClient.ApplicationMessageReceivedAsync += this.OnApplicationMessageReceivedAsync;
        this.mqttClient.ConnectedAsync += this.OnConnectedAsync;
        this.mqttClient.DisconnectedAsync += this.OnDisconnectedAsync;

        Console.WriteLine($"{nameof(this.Start)}: Connecting to broker...");

        try
        {
            await this.mqttClient.ConnectAsync(mqttClientOptions);
            this.reconnectAttempts = 0;
            return true;
        }
        catch
        {
            await this.CleanupClient();
            throw;
        }
    }

    private async Task CleanupClient()
    {
        if (this.mqttClient is null)
        {
            return;
        }

        Console.WriteLine($"{nameof(this.CleanupClient)}: Cleaning up MQTT client");

        try
        {
            if (this.mqttClient.IsConnected)
            {
                await this.mqttClient.UnsubscribeAsync(SisLinkWildcardTopic);
                await this.mqttClient.DisconnectAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{nameof(this.CleanupClient)}: An error occurred while disconnecting: {ex.Message}");
        }
        finally
        {
            this.mqttClient.ApplicationMessageReceivedAsync -= this.OnApplicationMessageReceivedAsync;
            this.mqttClient.ConnectedAsync -= this.OnConnectedAsync;
            this.mqttClient.DisconnectedAsync -= this.OnDisconnectedAsync;
            this.mqttClient.Dispose();
            this.mqttClient = null;
        }
    }

    public async Task Stop()
    {
        await this.semaphore.WaitAsync();

        try
        {
            this.isIntentionalDisconnect = true;
            await this.CleanupClient();
        }
        finally
        {
            this.isIntentionalDisconnect = false;
            this.semaphore.Release();
        }
    }

    private async Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());
        var topicParts = e.ApplicationMessage.Topic.Split('/');

        if (topicParts.Length < 2)
        {
            Console.WriteLine($"Message ignored with too short topic: {e.ApplicationMessage.Topic} -- Payload: {payload}");
            return;
        }

        if (topicParts.Contains("play"))
        {
            _audioPlayer.PlayAsync(null);
        }

        if (topicParts.Contains("stop"))
        {
            _audioPlayer.Stop();
        }

        if (topicParts.Contains("read") && topicParts.Contains("senderList"))
        {
            var path = ServiceListParser.DefaultServiceListPath;
            var serviceList = await ServiceListParser.ParseFileAsync(path);

            if (!serviceList.Success)
            {
                Console.WriteLine(serviceList.Message);
                return;
            }

            Console.WriteLine(serviceList.Message);

            var json = JsonSerializer.Serialize(new { Stations = serviceList.Services });

            await SendSenderlist(json);
        }

        // TODO check Topics
        //if (!this.forwardedSubTopics.Contains(topicParts[^2]))
        //{
        //    Console.WriteLine($"Message ignored with topic: {e.ApplicationMessage.Topic} and payload: {payload}");
        //    return;
        //}

        //if (payload == string.Empty)
        //{
        //    Console.WriteLine($"Empty Payload received for topic: {e.ApplicationMessage.Topic}");
        //    return;
        //}

        Console.WriteLine($"Received application message. Topic: {e.ApplicationMessage.Topic} -- Payload: {payload}");
    }

    public async Task SendSenderlist(string json)
    {
        //const string senderListPayload = """
        //                                 {
        //                                   "stations": [
        //                                     {
        //                                       "label": "Testsender",
        //                                       "mhz": 188.928,
        //                                       "serviceId": 3559,
        //                                       "subChannelId": 15,
        //                                       "strength": 200
        //                                     },
        //                                     {
        //                                       "label": "Testsender 2",
        //                                       "mhz": 188.928,
        //                                       "serviceId": 3577,
        //                                       "subChannelId": 6,
        //                                       "strength": -27
        //                                     }
        //                                   ]
        //                                 }
        //                                 """;

        await this.mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic("sislink/l008/sl008u04/slp001/status/dab/senderList")
            .WithPayload(json)
            .Build());
    }


    private async Task OnConnectedAsync(MqttClientConnectedEventArgs e)
    {
        Console.WriteLine("Connected to MQTT broker");

        var topicFilter = new MqttTopicFilterBuilder().WithTopic(SisLinkWildcardTopic).Build();

        await this.mqttClient.SubscribeAsync(topicFilter);
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        if (this.isIntentionalDisconnect || this.isDisposed)
        {
            Console.WriteLine("Intentional disconnect or disposed, skipping reconnection");
            return;
        }

        Console.WriteLine($"Unexpected disconnect. Reason: {args.Reason}");

        await this.Reconnect();
    }

    private async Task Reconnect()
    {
        while (!this.isDisposed)
        {
            if (this.reconnectAttempts < int.MaxValue)
            {
                this.reconnectAttempts++;
            }

            var delay = Math.Min(InitialReconnectDelayMs * (int) Math.Pow(2, Math.Min(this.reconnectAttempts - 1, 6)), MaxReconnectDelayMs);

            Console.WriteLine($"Reconnection attempt {this.reconnectAttempts} in {delay}ms");

            await Task.Delay(delay);

            await this.semaphore.WaitAsync();

            try
            {
                await this.CleanupClient();
                await this.ConnectClient();
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reconnection attempt {this.reconnectAttempts} failed: {ex.Message}");
            }
            finally
            {
                this.semaphore.Release();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (this.isDisposed)
        {
            return;
        }

        this.isDisposed = true;

        await this.Stop();
        this.semaphore.Dispose();
    }
}