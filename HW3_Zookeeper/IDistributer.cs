using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HW3_Zookeeper
{
    interface IDistributer
    {

        void join(String alliance, String airline, String url);
        bool isDelegate();
        List<NodeData> getAirlines(String alliance);

    }
}
