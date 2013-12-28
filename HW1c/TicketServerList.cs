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
        static public ConcurrentDictionary<string, IAirlineServerSoap> ticketServersProxies =
        new ConcurrentDictionary<string, IAirlineServerSoap>();
        static public ConcurrentDictionary<string, string> airlineToServer =
        new ConcurrentDictionary<string, string>();
    }
}
