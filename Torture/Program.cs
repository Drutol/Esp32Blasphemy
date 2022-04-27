using System;
using System.Collections;
using System.Device.WiFi;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using nanoFramework.json;
using nanoFramework.Json;
using nanoFramework.Networking;
using Torture.Infrastructure;

namespace Torture
{
    public class Program
    {
        public static void Main()
        {
            try
            {
                //var wifi =
                //    WiFiNetworkHelper.ScanAndConnectDhcp(
                //        "LajFaj",
                //        "---",
                //        WiFiReconnectionKind.Automatic,
                //        true);
                //Debug.WriteLine($"Wifi status: {wifi}.");

                if (true)
                {
                    Debug.WriteLine($"My ip: {NetworkInterface.GetAllNetworkInterfaces()[0].IPv4Address}");

                    var httpPublisher = new HttpPublisher();
                    var mqttPublisher = new MqttPublisher("192.168.0.100");

                    var nfcDataProvider = new NfcDataProvider();

                    nfcDataProvider.Start();
                    httpPublisher.Start();
                    mqttPublisher.Start();
                    
                    while (true)
                    {
                        var message = nfcDataProvider.GetMessage();

                        if (message != null)
                        {
                            httpPublisher.Publish(message);
                            mqttPublisher.Publish(message);
                        }

                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }


                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            Thread.Sleep(Timeout.Infinite);
        }
    }
}


//Debug.WriteLine($"Sending request...");
//var client = new HttpClient
//{
//    HttpsAuthentCert = new X509Certificate(Resources.GetString(Resources.StringResources.Cert))
//};

//var response = client.Get("https://baconipsum.com/api/?type=all-meat&paras=2&start-with-lorem=1");

//Debug.WriteLine($"Got response, status code: {response.StatusCode}");

//if (response.IsSuccessStatusCode)
//{
//    var ipsums = (ArrayList)JsonConvert.DeserializeObject(response.Content.ReadAsStream(), typeof(ArrayList));

//    foreach (string ipsum in ipsums)
//    {
//        Debug.WriteLine(ipsum);
//    }
//}