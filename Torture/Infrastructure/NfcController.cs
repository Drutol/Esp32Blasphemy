using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using Common;
using nanoFramework.Hardware.Esp32;
using nanoFramework.Json;
using nanoFramework.WebServer;

namespace Torture.Infrastructure
{
    internal class NfcController
    {
        public static NfcDataMessage CurrentMessage { get; set; }
        public static Exception Error { get; set; }
        public static uint DataCounter { get; set; }
        public static bool MqttConnection { get; set; }
        public static uint NfcMissCounter { get; set; }

        private CpuStatsProvider _statsProvider;

        [Route("data")]
        [Method("GET")]
        public void GetNfcData(WebServerEventArgs e)
        {
            try
            {
                if (CurrentMessage == null)
                {
                    WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.NotFound);
                    return;
                }

                var json = JsonConvert.SerializeObject(CurrentMessage);
                CurrentMessage = null;
                e.Context.Response.ContentType = "application/json";
                WebServer.OutPutStream(e.Context.Response, json);
            }
            catch (Exception exception)
            {
               // client disconnected 
            }
        }    
        
        [Route("diag")]
        [Method("GET")]
        public void GetDiagData(WebServerEventArgs e)
        {
            try
            {
                var provider = new CpuStatsProvider();
                e.Context.Response.ContentType = "application/text";
                NativeMemory.GetMemoryInfo(NativeMemory.MemoryType.Internal, out var totalSize, out var freeSize, out var largestFreeBlock);
                WebServer.OutPutStream(e.Context.Response,
                                                            $"{Error}\n" +
                                                           $"Counter: {DataCounter}\n" +
                                                           $"Misses: {NfcMissCounter}\n" +
                                                           $"Mqtt: {MqttConnection}\n" +
                                                           $"Mem: {freeSize}/{totalSize} ({largestFreeBlock})\n" +
                                                           $"Cpu: {provider.GetCpuUsage()}");
            }
            catch (Exception exception)
            {
               // client disconnected 
            }
        }
    }
}
