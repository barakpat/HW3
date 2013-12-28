using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace HW3_Zookeeper
{
    public class ServerData
    {
        public String url { get ; set ; }
        public String airline { get; set; }
        public List<AirlineData> airlines { get; set; }

        public ServerData(String url, String airline, List<AirlineData> airlines)
        {
            this.url = url;
            this.airline = airline;
            this.airlines = airlines;
        }

        static public String serialize(ServerData obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        static public ServerData deserialize(String str)
        {
            return JsonConvert.DeserializeObject<ServerData>(str);
        }
    }
}
