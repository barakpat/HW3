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
        private static String deleteBarrierNodeName = "/delete";

        private static String serverNodeNamePrefix = "n_";

        private static String NODE_JOINED = "join";
        private static String NODE_FAILED = "fail";

        public delegate void LeaderDelegate();
        public delegate void UpdateDataToPhaseDelegate(int phase);
        public delegate List<ServerData> DeleteOldDataDelegate(List<ServerData> servers, String joinedAirline);

        private ZooKeeper zk;
        private AutoResetEvent connectedSignal = new AutoResetEvent(false);

        private String alliance;
        private String airline;
        private String url;
        private LeaderDelegate leaderDelegate;
        private UpdateDataToPhaseDelegate updateDataToPhaseDelegate;
        private DeleteOldDataDelegate deleteOldDataDelegate;
        
        private String ephemeralNodeName;
        private bool isLeader;
        private int phase = 0;
        private Dictionary<int, List<ServerData>> phases = new Dictionary<int,List<ServerData>>();

        public Distributer(String alliance, String airline, String url, LeaderDelegate leaderDelegate, UpdateDataToPhaseDelegate updateDataToPhaseDelegate, DeleteOldDataDelegate deleteOldDataDelegate)
        {
            this.alliance = alliance;
            this.airline = airline;
            this.url = url;
            this.leaderDelegate = leaderDelegate;
            this.updateDataToPhaseDelegate = updateDataToPhaseDelegate;
            this.deleteOldDataDelegate = deleteOldDataDelegate;
            
            this.zk = new ZooKeeper(getAddress(), new TimeSpan(1, 0, 0, 0), this);
            this.connectedSignal.WaitOne();

            createNodeIfNotExists(rootNodeName, new byte[0]);
            createNodeIfNotExists(barriersNodeName, new byte[0]);

            String alliancePath = rootNodeName + "/" + this.alliance;
            String phaseBarrierPath = barriersNodeName + phaseBarrierNodeName;
            String alliancePhaseBarrierPath = phaseBarrierPath + "/" + this.alliance;
            String deleteBarrierPath = barriersNodeName + deleteBarrierNodeName;
            String allianceDeleteBarrierPath = deleteBarrierPath + "/" + this.alliance;

            createNodeIfNotExists(alliancePath, getBytes(AllianceData.serialize(new AllianceData())));
            createNodeIfNotExists(phaseBarrierPath, new byte[0]);
            createNodeIfNotExists(alliancePhaseBarrierPath, new byte[0]);
            createNodeIfNotExists(deleteBarrierPath, new byte[0]);
            createNodeIfNotExists(allianceDeleteBarrierPath, new byte[0]);
        }

        public void Process(WatchedEvent @event)
        {
            if (@event.State == KeeperState.SyncConnected && @event.Type == EventType.None)
            {
                this.connectedSignal.Set();
            }
            if (@event.Type == EventType.NodeChildrenChanged)
            {            
                algorithm(false);
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

            setServers(this.phase, new List<ServerData>() { serverNode });

            algorithm(true);
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

        private void algorithm(bool isIJoined)
        {
            String allianceInServersNode = rootNodeName + "/" + this.alliance;
            
            IEnumerable<String> serverNodes = this.zk.GetChildren(allianceInServersNode, true);

            setLeader(allianceInServersNode, serverNodes);

            Console.WriteLine("Start update phase");
            
            String phaseBarrierPath = barriersNodeName + phaseBarrierNodeName + "/" + this.alliance;
            LeaderBarrier phaseBarrier = new LeaderBarrier(getAddress(), phaseBarrierPath, serverNodes.Count() - 1);

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
                    Stat s = new Stat();
                    ServerData serverData = ServerData.deserialize(getString(this.zk.GetData(airlineInServerNode, false, s)));
                    foreach (ServerData sd in this.phases[newPhase])
                    {
                        if (serverData.airline.Equals(sd.airline))
                        {
                            this.zk.SetData(airlineInServerNode, getBytes(ServerData.serialize(sd)), -1);
                            break;
                        }
                    }
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
                setServers(newPhase, this.phases[this.phase]);
            }
                
            this.phase = newPhase;

            removeServers(newPhase);
            
            this.updateDataToPhaseDelegate(newPhase);

            Console.WriteLine("before leave");
            phaseBarrier.Leave();
            Console.WriteLine("after leave");

            Console.WriteLine("Start delete old data");

            String deleteBarrierPath = barriersNodeName + deleteBarrierNodeName + "/" + this.alliance;
            Barrier deleteBarrier = new Barrier(getAddress(), deleteBarrierPath, this.airline, serverNodes.Count());

            Console.WriteLine("before enter");            
            deleteBarrier.Enter();
            Console.WriteLine("after enter");

            if (!isIJoined)
            {
                String action = null;
                String airlineChanged = null;

                List<String> prevNodes = new List<string>();
                foreach (ServerData serverData in this.phases[this.phase])
                {
                    prevNodes.Add(serverData.airline);
                }

                List<String> currNodes = new List<string>();
                List<ServerData> serversData = new List<ServerData>();
                Dictionary<String, String> airlineToNodeName = new Dictionary<string, string>();
                foreach (String airline in serverNodes)
                {
                    String airlineInAllianceNode = allianceInServersNode + "/" + airline;
                    Stat s = new Stat();
                    ServerData sd = ServerData.deserialize(getString(this.zk.GetData(airlineInAllianceNode, false, s)));
                    currNodes.Add(sd.airline);
                    serversData.Add(sd);
                    airlineToNodeName[sd.airline] = airline;
                }

                if (currNodes.Count() > prevNodes.Count())
                {
                    action = NODE_JOINED;
                    airlineChanged = currNodes.Except(prevNodes).First();
                    Console.WriteLine("node joined - " + airlineChanged);
                }
                else
                {
                    action = NODE_FAILED;
                    airlineChanged = prevNodes.Except(currNodes).First();
                    Console.WriteLine("node failed - " + airlineChanged);
                }

                if (action.Equals(NODE_JOINED))
                {
                    List<ServerData> serversDataAfterDeletion = this.deleteOldDataDelegate(serversData, airlineChanged);

                    if (this.isLeader)
                    {
                        Console.WriteLine("leader");
                        foreach (ServerData serverData in serversDataAfterDeletion)
                        {
                            Console.WriteLine("set data to: " + airlineToNodeName[serverData.airline]);
                            this.zk.SetData(allianceInServersNode + "/" + airlineToNodeName[serverData.airline], getBytes(ServerData.serialize(serverData)), -1);
                            Console.WriteLine("after set data");
                        }
                    }
                }
            }
            
            Console.WriteLine("before leave");
            deleteBarrier.Leave();
            Console.WriteLine("after leave");
        
        }

        private void createNodeIfNotExists(String path, byte[] data)
        {
            if (this.zk.Exists(path, false) == null)
            {
                this.zk.Create(path, data, Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }
        }

        private void setLeader(String serversPath, IEnumerable<String> serverNodes)
        {
            List<string> serverNodesList = serverNodes.ToList();
            serverNodesList.Sort();

            if (serverNodesList[0].Equals(this.ephemeralNodeName))
            {
                if (!this.isLeader)
                {
                    this.leaderDelegate();
                }
                this.isLeader = true;
            }
            else
            {
                this.isLeader = false;
            }
        }

        
        private void setServers(int currPhase, List<ServerData> servers)
        {
            this.phases[currPhase] = servers;        
        }

        private void removeServers(int currPhase)
        {
            foreach (int key in this.phases.Keys)
            {
                if (key != currPhase)
                {
                    this.phases.Remove(key);
                }
            }
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
