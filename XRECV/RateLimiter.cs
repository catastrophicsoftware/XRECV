using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;

namespace XRECV
{
    public sealed class ClientRateInformation
    {

    }

    public sealed class RateLimiter
    {
        private Dictionary<string, ClientRateInformation> rateData;

        public RateLimiter()
        {

        }
    }
}
