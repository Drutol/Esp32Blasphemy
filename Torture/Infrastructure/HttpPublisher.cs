using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using Common;
using nanoFramework.WebServer;
using Torture.Interfaces;

namespace Torture.Infrastructure
{
    internal class HttpPublisher : IDataPublisher
    {
        private readonly Thread _serverThread;

        public HttpPublisher()
        {
            _serverThread = new Thread(ServerLoop);
        }

        private void ServerLoop()
        {
            using var server = new WebServer(80, HttpProtocol.Http, new[] { typeof(NfcController) });
            
            server.Start();
            Debug.WriteLine("Started http server.");
            Thread.Sleep(Timeout.Infinite);
        }

        public void Start()
        {
            _serverThread.Start();
        }

        public void Publish(NfcDataMessage message)
        {
            NfcController.CurrentMessage = message;
        }
    }
}
