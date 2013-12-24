using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace HW1c
{
    class TicketServerList
    {
        static public ConcurrentDictionary<string, ITicketSellingServerSoap> ticketServersProxies =
        new ConcurrentDictionary<string, ITicketSellingServerSoap>();
    }
}
