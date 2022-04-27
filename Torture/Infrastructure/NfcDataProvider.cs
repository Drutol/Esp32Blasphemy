using System;
using System.Collections;
using System.Device.I2c;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using Common;
using Iot.Device.Pn532;
using Iot.Device.Pn532.ListPassive;
using nanoFramework.Hardware.Esp32;

namespace Torture.Infrastructure
{
    internal class NfcDataProvider
    {
        private readonly Queue _messageQueue = new();
        private readonly Thread _deviceThread;
        private readonly Pn532 _device;

        public NfcDataProvider()
        {
            //Configuration.SetPinFunction(21, DeviceFunction.I2C1_DATA);
            //Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);
            //_device = new Pn532(I2cDevice.Create(new I2cConnectionSettings(1, Pn532.I2cDefaultAddress)));

            Configuration.SetPinFunction(22, DeviceFunction.COM2_TX);
            Configuration.SetPinFunction(21, DeviceFunction.COM2_RX);
            
            while (true)
            {
                Thread.Sleep(1000);
                try
                {
                    _device = new Pn532("COM2");
                    break;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }
            
            _deviceThread = new Thread(NfcLoop);
        }

        private void NfcLoop()
        {
            while (true)
            {
                Thread.Sleep(TimeSpan.FromSeconds(2));

                byte[] retData;
                while (true)
                {
                    retData = _device.ListPassiveTarget(MaxTarget.One, TargetBaudRate.B106kbpsTypeA);
                    if (retData is { Length: >= 3 }) 
                        break;

                    // Give time to PN532 to process
                    Thread.Sleep(200);
                }

                var type = (PollingType)retData[1];
                Debug.WriteLine($"Num tags: {retData[0]}, Type: {type}");
                var tag = _device.TryDecode106kbpsTypeA(new SpanByte(retData).Slice(1));

                _messageQueue.Enqueue(new NfcDataMessage
                {
                    DateTime = DateTime.UtcNow,
                    NfcData = $"{tag.Atqa} {BitConverter.ToString(tag.NfcId)}"
                });
            }
        }

        public void Start()
        {
            _deviceThread.Start();
        }

        public NfcDataMessage GetMessage()
        {
            if (_messageQueue.Count == 0)
                return null;

            return (NfcDataMessage)_messageQueue.Dequeue();
        }
    }
}
