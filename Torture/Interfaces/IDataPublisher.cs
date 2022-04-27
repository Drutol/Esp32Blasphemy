using System;
using System.Collections.Generic;
using System.Text;
using Common;

namespace Torture.Interfaces
{
    internal interface IDataPublisher
    {
        void Publish(NfcDataMessage message);
    }
}
