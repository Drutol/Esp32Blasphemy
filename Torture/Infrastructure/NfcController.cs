using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Common;
using nanoFramework.Json;
using nanoFramework.WebServer;

namespace Torture.Infrastructure
{
    internal class NfcController
    {
        public static NfcDataMessage CurrentMessage { get; set; }

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
    }
}
