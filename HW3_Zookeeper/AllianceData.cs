using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace HW3_Zookeeper
{
    public class AllianceData
    {
        public List<String> airlines { get; set; }

        public AllianceData()
        {
            this.airlines = new List<string>();
        }

        static public String serialize(AllianceData obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        static public AllianceData deserialize(String str)
        {
            return JsonConvert.DeserializeObject<AllianceData>(str);
        }
    }
}
