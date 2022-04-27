using System.Net.Http.Json;
using System.Text;
using Common;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

_ = Task.Factory.StartNew(
    MqttServerLoop, 
    CancellationToken.None,
    TaskCreationOptions.LongRunning,
    TaskScheduler.Default);

_ = Task.Factory.StartNew(
    HttpPollingLoop, 
    CancellationToken.None,
    TaskCreationOptions.LongRunning,
    TaskScheduler.Default);

await Task.Delay(-1);


async void MqttServerLoop()
{
    var mqttServer = new MqttFactory().CreateMqttServer();
    var opts = new MqttServerOptionsBuilder()
        .WithConnectionValidator(context =>
        {
            Console.WriteLine($"New connection from: {context.ClientId}");
            context.ReasonCode = MqttConnectReasonCode.Success;
        }).WithApplicationMessageInterceptor(context =>
        {
            using var ms = new MemoryStream(context.ApplicationMessage.Payload);
            using var reader = new BinaryReader(ms);

            var message = new NfcDataMessage
            {
                DateTime = new DateTime(reader.ReadInt64(), DateTimeKind.Utc),
                NfcData = Encoding.UTF8.GetString(reader.ReadBytes((int)(ms.Length - ms.Position)))
            };

            var log = $"Mqtt;{DateTime.UtcNow};{message.DateTime};{message.NfcData}";
            Console.WriteLine(log);
            File.AppendAllLines("MqttMessages.csv", new [] {log});
        });
    await mqttServer.StartAsync(opts.Build());
}

async void HttpPollingLoop()
{
    var client = new HttpClient();
    while (true)
    {
        try
        {
            await Task.Delay(1000);

            var message = await client.GetFromJsonAsync<NfcDataMessage>("http://192.168.0.114/data");

            if (message != null)
            {
                var log = $"Http;{DateTime.UtcNow};{message.DateTime};{message.NfcData}";
                Console.WriteLine(log);
                File.AppendAllLines("HttpMessages.csv", new[] { log });
            }
        }
        catch (Exception e)
        {

        }
    }
}
