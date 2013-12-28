using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HW3_Zookeeper
{
    interface IDistributer
    {
        void join();
        void leave();
        bool isDelegate();
        List<ServerData> getServers();
    }
}
