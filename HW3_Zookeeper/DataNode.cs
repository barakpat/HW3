using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace HW3_Zookeeper
{
    public class DataNode
    {
        public String url { get ; set ; }
        public String airline { get; set; }
        public List<String> storedAirlines { get; set; }
        public List<String> myAirlineLocation { get; set; }

        public DataNode(String url, String airline, List<String> storedAirlines, List<String> myAirlineLocation)
        {
            this.url = url;
            this.airline = airline;
            this.storedAirlines = storedAirlines;
            this.myAirlineLocation = myAirlineLocation;
        }

        static public String serialize(DataNode obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        static public DataNode deserialize(String str)
        {
            return JsonConvert.DeserializeObject<DataNode>(str);
        }
    }
}
