using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HW3_Zookeeper
{
    interface IDistributer
    {
        void join();
        bool isDelegate();
        List<ServerNode> getServers();
        List<DataNode> getData();
    }
}
