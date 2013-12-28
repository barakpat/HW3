using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZooKeeperNet;
using Org.Apache.Zookeeper.Data;
using System.Threading;

//TODO: what to do when all the servers inside alliance failed? remove the alliance node? inform the search server?

namespace HW3_Zookeeper
{

    public class Distributer : IDistributer, IWatcher
    {

        private static String zookeeperConfigFilePath = "zookeeper.conf";
        
        private static String rootNodeName = "/hw3";
        private static String serversNodeName = "/servers";
        private static String dataNodeName = "/data";
        
        private static String serverNodeNamePrefix = "n_";

        private ZooKeeper zk;
        
        private AutoResetEvent connectedSignal = new AutoResetEvent(false);

        private String alliance;
        private String airline;
        private String url;
        
        private String sequence;
        private bool isLeader;
        
        private List<DataNode> data;
        private List<ServerNode> servers;

        public Distributer(String alliance, String airline, String url)
        {
            this.alliance = alliance;
            this.airline = airline;
            this.url = url;
            
            this.zk = new ZooKeeper(getAddress(), new TimeSpan(1, 0, 0, 0), this);
            this.connectedSignal.WaitOne();
           
            Stat s = this.zk.Exists(rootNodeName, false);
            if (s == null)
            {
                this.zk.Create(rootNodeName, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }

            String serversNode = rootNodeName + serversNodeName;
            s = this.zk.Exists(serversNode, false);
            if (s == null)
            {
                this.zk.Create(serversNode, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }

            String allianceInServersNode = serversNode + "/" + alliance;
            s = this.zk.Exists(allianceInServersNode, false);
            if (s == null)
            {
                this.zk.Create(allianceInServersNode, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }

            String dataNode = rootNodeName + dataNodeName;
            s = this.zk.Exists(dataNode, false);
            if (s == null)
            {
                this.zk.Create(dataNode, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }

            String allianceInDataNode = dataNode + "/" + alliance;
            s = this.zk.Exists(allianceInDataNode, false);
            if (s == null)
            {
                this.zk.Create(allianceInDataNode, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }
        }

        public void Process(WatchedEvent @event)
        {
            if (@event.State == KeeperState.SyncConnected && @event.Type == EventType.None)
            {
                this.connectedSignal.Set();
            }
            if (@event.Type == EventType.NodeChildrenChanged && @event.Path.StartsWith(rootNodeName + serversNodeName))
            {            
                handleChange();
            }
        }

        public void join()
        {
            ServerNode serverNode = new ServerNode(this.url, this.airline);

            String airlineInServersNode = rootNodeName + serversNodeName + "/" + this.alliance + "/" + serverNodeNamePrefix;

            String pathToNewServerNode = this.zk.Create(airlineInServersNode, getBytes(ServerNode.serialize(serverNode)), Ids.OPEN_ACL_UNSAFE, CreateMode.EphemeralSequential);
            this.sequence = pathToNewServerNode.Replace(airlineInServersNode, "");

            DataNode dataNode = new DataNode(this.url, this.airline, new List<String>() { this.airline }, new List<String>() { this.url });

            String airlineInDataNode = rootNodeName + dataNodeName + "/" + this.alliance + "/" + this.airline;
            
            Stat s = this.zk.Exists(airlineInDataNode, false);
            if (s == null)
            {
                this.zk.Create(airlineInDataNode, getBytes(DataNode.serialize(dataNode)), Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }
            else
            {
                this.zk.SetData(airlineInDataNode, getBytes(DataNode.serialize(dataNode)), -1);
            }

            handleChange();
        }

        public bool isDelegate()
        {
            return this.isLeader;
        }

        public List<DataNode> getData()
        {
            return this.data;
        }

        public List<ServerNode> getServers()
        {
            return this.servers;
        }

        private void handleChange()
        {
            String allianceInServersNode = rootNodeName + serversNodeName + "/" + this.alliance;
            String allianceInDataNode = rootNodeName + dataNodeName + "/" + this.alliance;

            IEnumerable<String> serverNodes = this.zk.GetChildren(allianceInServersNode, true);
            IEnumerable<String> dataNodes = this.zk.GetChildren(allianceInDataNode, false);

            setLeader(allianceInServersNode, serverNodes);
            setServers(allianceInServersNode, serverNodes);
            setData(allianceInDataNode, dataNodes);

            //TODO: run change procedure
        }

        private void setLeader(String serversPath, IEnumerable<String> serverNodes)
        {
            List<string> serverNodesList = serverNodes.ToList();
            serverNodesList.Sort();

            if (serverNodesList[0].Replace(serverNodeNamePrefix, "").Equals(this.sequence))
            {
                //TODO: Run leader procedure
                this.isLeader = true;
            }
            else
            {
                this.isLeader = false;
            }
        }

        private void setData(String dataPath, IEnumerable<String> dataNodes)
        {
            List<DataNode> _data = new List<DataNode>();

            foreach (String dataNode in dataNodes)
            {
                String airlineInDataNode = dataPath + "/" + dataNode;
                Stat s = new Stat();
                _data.Add(DataNode.deserialize(getString(this.zk.GetData(airlineInDataNode, false, s))));
            }

            data = _data;
        }

        private void setServers(String serversPath, IEnumerable<String> serverNodes)
        {
            List<ServerNode> _servers = new List<ServerNode>();

            foreach (String serverNode in serverNodes)
            {
                String airlineInServerNode = serversPath + "/" + serverNode;
                Stat s = new Stat();
                _servers.Add(ServerNode.deserialize(getString(this.zk.GetData(airlineInServerNode, false, s))));
            }

            servers = _servers;
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

    }

}
