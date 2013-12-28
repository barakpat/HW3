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
        
        private static String rootNodeName = "/alliances";
        private static String barriersNodeName = "/barriers";
        private static String phaseBarrierNodeName = "/phase";

        private static String serverNodeNamePrefix = "n_";

        public delegate void UpdateDataToPhaseDelegate(int phase);

        private ZooKeeper zk;
        private AutoResetEvent connectedSignal = new AutoResetEvent(false);

        private String alliance;
        private String airline;
        private String url;
        private UpdateDataToPhaseDelegate updateDataToPhaseDelegate;
        
        private String ephemeralNodeName;
        private bool isLeader;
        private int phase = 0;
        private Dictionary<int, List<ServerData>> phases = new Dictionary<int,List<ServerData>>();

        public Distributer(String alliance, String airline, String url, UpdateDataToPhaseDelegate updateDataToPhaseDelegate)
        {
            this.alliance = alliance;
            this.airline = airline;
            this.url = url;
            this.updateDataToPhaseDelegate = updateDataToPhaseDelegate;
            
            this.zk = new ZooKeeper(getAddress(), new TimeSpan(1, 0, 0, 0), this);
            this.connectedSignal.WaitOne();

            if (this.zk.Exists(rootNodeName, false) == null)
            {
                this.zk.Create(rootNodeName, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }

            String alliancePath = rootNodeName + "/" + this.alliance;
            if (this.zk.Exists(alliancePath, false) == null)
            {
                this.zk.Create(alliancePath, getBytes(AllianceData.serialize(new AllianceData())), Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }

            if (this.zk.Exists(barriersNodeName, false) == null)
            {
                this.zk.Create(barriersNodeName, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }

            String phaseBarrierPath = barriersNodeName + phaseBarrierNodeName;
            if (this.zk.Exists(phaseBarrierPath, false) == null)
            {
                this.zk.Create(phaseBarrierPath, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }
        }

        public void Process(WatchedEvent @event)
        {
            if (@event.State == KeeperState.SyncConnected && @event.Type == EventType.None)
            {
                this.connectedSignal.Set();
            }
            if (@event.Type == EventType.NodeChildrenChanged)
            {            
                algorithm();
            }
        }

        public void join()
        {
            AirlineData airlineData = new AirlineData(this.airline, true);
            ServerData serverNode = new ServerData(this.url, this.airline, new List<AirlineData>() { airlineData });
            String allianceInRootNode = rootNodeName + "/" + this.alliance;
            this.ephemeralNodeName = this.zk.Create(allianceInRootNode + "/" + serverNodeNamePrefix, getBytes(ServerData.serialize(serverNode)), Ids.OPEN_ACL_UNSAFE, CreateMode.EphemeralSequential).Replace(allianceInRootNode + "/", "");

            Stat s = new Stat();
            AllianceData allianceData = AllianceData.deserialize(getString(this.zk.GetData(allianceInRootNode, true, s)));
            if (!allianceData.airlines.Contains(this.airline))
            {
                allianceData.airlines.Add(this.airline);
                this.zk.SetData(allianceInRootNode, getBytes(AllianceData.serialize(allianceData)), -1);
            }

            this.phases[this.phase] = new List<ServerData>() { serverNode };

            algorithm();
        }

        public void leave()
        {
            this.zk.Delete(rootNodeName + "/" + this.alliance + "/" + this.ephemeralNodeName, -1);
        }

        public bool isDelegate()
        {
            return this.isLeader;
        }

        public List<ServerData> getServers()
        {
            return this.phases[this.phase];
        }

        private void algorithm()
        {
            String allianceInServersNode = rootNodeName + "/" + this.alliance;
            
            IEnumerable<String> serverNodes = this.zk.GetChildren(allianceInServersNode, true);

            setLeader(allianceInServersNode, serverNodes);

            String phaseBarrierPath = barriersNodeName + phaseBarrierNodeName;
            Barrier phaseBarrier = new Barrier(getAddress(), phaseBarrierPath, serverNodes.Count() - 1);

            int newPhase = this.phase;
            
            if (isLeader)
            {
                Console.WriteLine("leader");
                Console.WriteLine("before enter");
                phaseBarrier.Enter();
                Console.WriteLine("after enter");

                IEnumerable<String> phaseBarrierChildren = this.zk.GetChildren(phaseBarrierPath, false);

                List<int> phases = new List<int>() { this.phase };
                foreach (String airline in phaseBarrierChildren)
                {
                    String airlineInBarrierNode = phaseBarrierPath + "/" + airline;
                    Stat s = new Stat();
                    phases.Add(Convert.ToInt32(getString(this.zk.GetData(airlineInBarrierNode, false, s))));
                }

                newPhase = phases.Max();
                foreach (String airline in phaseBarrierChildren)
                {
                    String airlineInBarrierNode = phaseBarrierPath + "/" + airline;
                    Stat s = new Stat();
                    Console.WriteLine("before send to: " + airline);
                    this.zk.SetData(airlineInBarrierNode, getBytes(newPhase.ToString()), -1);
                    Console.WriteLine("after send to: " + airline);
                }

                //set zookeeper with new phase data
                foreach (String serverNode in serverNodes)
                {
                    String airlineInServerNode = allianceInServersNode + "/" + serverNode;
                    ServerData serverData = null;
                    foreach (ServerData sd in this.phases[newPhase])
                    {
                        if (sd.airline.Equals(this.airline))
                        {
                            serverData = sd;
                            break;
                        }
                    }
                    this.zk.SetData(airlineInServerNode, getBytes(ServerData.serialize(serverData)), -1);
                }

            }
            else
            {
                Console.WriteLine("not leader");
                String phaseBarrierPathToAirline = phaseBarrierPath + "/" + this.airline;
                DataChangedWatch dataChangedWatch = new DataChangedWatch(getAddress(), phaseBarrierPathToAirline);

                this.zk.Create(phaseBarrierPathToAirline, getBytes(phase.ToString()), Ids.OPEN_ACL_UNSAFE, CreateMode.Ephemeral);

                Console.WriteLine("before wait");
                dataChangedWatch.Wait();
                Console.WriteLine("after wait");

                Stat stat = new Stat();
                newPhase = Convert.ToInt32(getString(this.zk.GetData(phaseBarrierPathToAirline, false, stat)));

                this.zk.Delete(phaseBarrierPathToAirline, -1);
            }

            if (newPhase > 0 && this.phase == 0)
            {
                this.phases[newPhase] = this.phases[this.phase];
            }
                
            this.phase = newPhase;

            foreach (int key in this.phases.Keys)
            {
                if (key != newPhase)
                {
                    this.phases.Remove(key);
                }
            }
            
            this.updateDataToPhaseDelegate(newPhase);

            Console.WriteLine("before leave");
            phaseBarrier.Leave();
            Console.WriteLine("after leave");

        }

        private void setLeader(String serversPath, IEnumerable<String> serverNodes)
        {
            List<string> serverNodesList = serverNodes.ToList();
            serverNodesList.Sort();

            if (serverNodesList[0].Equals(this.ephemeralNodeName))
            {
                if (!this.isLeader)
                {
                    //TODO: Run leader procedure
                }
                this.isLeader = true;
            }
            else
            {
                this.isLeader = false;
            }
        }

        /*
        private void setServers(String serversPath, IEnumerable<String> serverNodes)
        {
            List<ServerData> _servers = new List<ServerData>();

            foreach (String serverNode in serverNodes)
            {
                String airlineInServerNode = serversPath + "/" + serverNode;
                Stat s = new Stat();
                _servers.Add(ServerData.deserialize(getString(this.zk.GetData(airlineInServerNode, false, s))));
            }

            servers = _servers;
        }
        */

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
