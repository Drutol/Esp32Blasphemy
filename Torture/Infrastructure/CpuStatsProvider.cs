using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Torture.Infrastructure
{
    public class CpuStatsProvider
    {
        private readonly sbyte[] _statsBuffer = new sbyte[1024];

        public string GetCpuUsage()
        {
            var length = GetCpuUsageInternal(_statsBuffer);
            var charArray = new byte[length];
            for (int i = 0; i < length; i++)
                charArray[i] = (byte)_statsBuffer[i];

            var str = Encoding.UTF8.GetString(charArray, 0, length);

            return str;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern ushort GetCpuUsageInternal(sbyte[] buffer);
    }
}
