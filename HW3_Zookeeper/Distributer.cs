using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZooKeeperNet;
using Org.Apache.Zookeeper.Data;

namespace HW3_Zookeeper
{

    public class Distributer : IDistributer, IWatcher
    {

        private static String zookeeperConfigFilePath = "zookeeper.conf";
        private static String root = "/alliances";
        private static String airlines = "/airlines";
        private static String data = "/data";

        private ZooKeeper zk;

        public Distributer()
        {
            zk = new ZooKeeper(getAddress(), new TimeSpan(1, 0, 0, 0), this);
            Stat s = zk.Exists(root, false);
            if (s == null)
            {
                zk.Create(root, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }
        }

        public void Process(WatchedEvent @event)
        {
            if (@event.Type == EventType.NodeCreated)
            {
            }
            
            int i = 1;
            //throw new NotImplementedException();
        }

        public void join(String alliance, String airline, String url)
        {
            String allianceNode = root + "/" + alliance;
            String airlinesNode = allianceNode + airlines;
            String dataNode = allianceNode + data;
            
            Stat s = zk.Exists(allianceNode, false);
            if (s == null)
            {
                zk.Create(allianceNode, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
                zk.Create(airlinesNode, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
                zk.Create(dataNode, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }

            String airlineInAirlinesNode = airlinesNode + "/" + airline;
            
            s = zk.Exists(airlineInAirlinesNode, false);
            if (s == null)
            {
                zk.Create(airlineInAirlinesNode, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Ephemeral);
            }
            else
            {
                //TODO: not suppose to happen
            }

            NodeData nodeData = new NodeData(url, airline, new List<String>() { airline }, new List<String>() { url });

            String airlineInDataNode = dataNode + "/" + airline;
            
            s = zk.Exists(airlineInDataNode, false);
            if (s == null)
            {
                zk.Create(airlineInDataNode, getBytes(NodeData.serialize(nodeData)), Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }
            else
            {
                //TODO: update the data in the node
            }
        }

        public bool isDelegate()
        {
            return true;
        }

        public List<NodeData> getAirlines(String alliance)
        {
            String allianceNode = root + "/" + alliance;
            String dataNode = allianceNode + data;

            if (zk.Exists(dataNode, false) == null)
            {
                //TODO: not suppose to happen
                return new List<NodeData>();
            }

            IEnumerable<String> airlines = zk.GetChildren(dataNode, false);

            List<NodeData> airlinesData = new List<NodeData>();

            foreach (String airline in airlines)
            {
                String airlineInDataNode = dataNode + "/" + airline;
                Stat s = zk.Exists(airlineInDataNode, false);
                airlinesData.Add(NodeData.deserialize(getString(zk.GetData(airlineInDataNode, false, s))));
            }

            return airlinesData;
        }

        private static String getAddress()
        {
            System.IO.StreamReader file = new System.IO.StreamReader(zookeeperConfigFilePath);
            String address = file.ReadLine();
            file.Close();
            return address;
        }

        private static byte[] getBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static string getString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        public static void Main()
        {
            Distributer d = new Distributer();
            d.join("Matmid", "ElAl", "127.0.0.1:8080");
            List<NodeData> nodes = d.getAirlines("Matmid");
            Console.ReadKey();
        }

    }

}
