using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Common;
using nanoFramework.M2Mqtt;
using nanoFramework.M2Mqtt.Messages;
using Torture.Interfaces;

namespace Torture.Infrastructure
{
    internal class MqttPublisher : IDataPublisher
    {
        private bool _isReconnecting = false;

        private readonly Thread _publishThread;
        
        private readonly MqttClient _mqttClient;

        private Thread _connectionThread;
        private NfcDataMessage _currentMessage;

        public MqttPublisher(string brokerAddress)
        {
            _mqttClient = new MqttClient(brokerAddress);
            _publishThread = new Thread(PublishLoop);
            _connectionThread = new Thread(ConnectionLoop);
            _mqttClient.ConnectionClosed += MqttClientOnConnectionClosed;
        }

        public void Start()
        {
            _publishThread.Start();
            _connectionThread.Start();
        }

        private void MqttClientOnConnectionClosed(object sender, EventArgs e)
        {
            if(_isReconnecting)
                return;

            NfcController.MqttConnection = false;
            _connectionThread = new Thread(ConnectionLoop);
            _connectionThread.Start();
        }

        private void ConnectionLoop()
        {
            if(_isReconnecting)
                return;

            _isReconnecting = true;

            while (true)
            {
                try
                {
                    var result = _mqttClient.Connect($"Esp32-{Guid.NewGuid()}");
                    if (result != MqttReasonCode.Success)
                    {
                        Debug.WriteLine($"Failed to connect to mqtt broker: {result}");
                    }
                    else // success
                    {
                        NfcController.MqttConnection = true;
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Failed to connect to mqtt broker: {e}");
                }

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }

            _isReconnecting = false;
            _connectionThread = null;
        }

        public void Publish(NfcDataMessage message)
        {
            _currentMessage = message;
        }

        private void PublishLoop()
        {
            while (true)
            {
                try
                {
                    var message = _currentMessage;
                    _currentMessage = null;
                    if (message != null)
                    {
                        var ticks = BitConverter.GetBytes(message.DateTime.Ticks);
                        var data = Encoding.UTF8.GetBytes(message.NfcData);

                        using var ms = new MemoryStream();
                        ms.Write(ticks, 0, ticks.Length);
                        ms.Write(data, 0, data.Length);

                        if (_mqttClient.IsConnected)
                        {
                            var id = _mqttClient.Publish($"/Esp32/{nameof(NfcDataMessage)}", ms.ToArray());
                            Debug.WriteLine($"Sent mqtt message with id: {id}");
                        }
                    }
                    else
                    {
                        Thread.Sleep(500);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }
        }
    }
}
