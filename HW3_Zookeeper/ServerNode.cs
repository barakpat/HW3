using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace HW3_Zookeeper
{
    public class ServerNode
    {
        public String url { get ; set ; }
        public String airline { get; set; }

        public ServerNode(String url, String airline)
        {
            this.url = url;
            this.airline = airline;
        }

        static public String serialize(ServerNode obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        static public ServerNode deserialize(String str)
        {
            return JsonConvert.DeserializeObject<ServerNode>(str);
        }
    }
}
