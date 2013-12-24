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

        static String zookeeperConfigFilePath = "zookeeper.conf";
        static String root = "/alliances";
        static String airlines = "/airlines";
        static String data = "/data";
        ZooKeeper zk = null;

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
            throw new NotImplementedException();
        }

        public void join(String alliance, String airline)
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

            String airlineInDataNode = dataNode + "/" + airline;

            s = zk.Exists(airlineInDataNode, false);
            if (s == null)
            {
                zk.Create(airlineInDataNode, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }
            else
            {
                //TODO: update the data in the node
            }

        }

        private String getAddress()
        {
            System.IO.StreamReader file = new System.IO.StreamReader(zookeeperConfigFilePath);
            String address = file.ReadLine();
            file.Close();
            return address;
        }

        public static void Main()
        {
            Distributer d = new Distributer();
            d.join("Matmid", "ElAl");
            Console.ReadKey();
            d.join("SkyTeam", "Alitalia");
            Console.ReadKey();
        }

    }

}
