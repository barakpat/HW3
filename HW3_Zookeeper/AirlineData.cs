using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HW3_Zookeeper
{
    public class AirlineData
    {
        public String name { get; set; }
        public bool isPrimary { get; set; }

        public AirlineData(String name, bool isPrimary)
        {
            this.name = name;
            this.isPrimary = isPrimary;
        }
    }
}
