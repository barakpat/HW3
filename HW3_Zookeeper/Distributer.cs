using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZooKeeperNet;
using Org.Apache.Zookeeper.Data;
using System.Threading;

//TODO: remove leader's zookeeper update between the leave and enter to make sure that everybody works on the previous verion of data

namespace HW3_Zookeeper
{

    public class Distributer : IDistributer, IWatcher
    {

        private static String zookeeperConfigFilePath = "zookeeper.conf";
        
        private static String rootNodeName = "/alliances";
        private static String barriersNodeName = "/barriers";
        private static String phaseBarrierNodeName = "/phase";
        private static String deleteBarrierNodeName = "/delete";
        private static String backupBarrierNodeName = "/backup";
        private static String balanceBarrierNodeName = "/balance";

        private static String serverNodeNamePrefix = "n_";

        private static String NODE_JOINED = "join";
        private static String NODE_FAILED = "fail";

        public delegate void LeaderDelegate();
        public delegate void UpdateDataToPhaseDelegate(int phase);
        public delegate List<ServerData> DeleteOldDataDelegate(List<ServerData> servers, String joinedAirline);
        public delegate List<ServerData> BackupDelegate(List<ServerData> servers);
        public delegate List<ServerData> BalanceDelegate(List<ServerData> servers, List<String> airlines, int newPhase);

        private ZooKeeper zk;
        private AutoResetEvent connectedSignal = new AutoResetEvent(false);

        private String alliance;
        private String airline;
        private String url;
        private LeaderDelegate leaderDelegate;
        private UpdateDataToPhaseDelegate updateDataToPhaseDelegate;
        private DeleteOldDataDelegate deleteOldDataDelegate;
        private BackupDelegate backupDelegate;
        private BalanceDelegate balanceDelegate;
        
        private String ephemeralNodeName;
        private bool isLeader;
        private int phase = 0;
        private Dictionary<int, List<ServerData>> phases = new Dictionary<int,List<ServerData>>();

        public Distributer(String alliance, String airline, String url,
                           LeaderDelegate leaderDelegate,
                           UpdateDataToPhaseDelegate updateDataToPhaseDelegate,
                           DeleteOldDataDelegate deleteOldDataDelegate,
                           BackupDelegate backupDelegate,
                           BalanceDelegate balanceDelegate)
        {
            this.alliance = alliance;
            this.airline = airline;
            this.url = url;
            this.leaderDelegate = leaderDelegate;
            this.updateDataToPhaseDelegate = updateDataToPhaseDelegate;
            this.deleteOldDataDelegate = deleteOldDataDelegate;
            this.backupDelegate = backupDelegate;
            this.balanceDelegate = balanceDelegate;
            
            this.zk = new ZooKeeper(getAddress(), new TimeSpan(1, 0, 0, 0), this);
            this.connectedSignal.WaitOne();

            createNodeIfNotExists(rootNodeName, new byte[0]);
            createNodeIfNotExists(barriersNodeName, new byte[0]);

            String alliancePath = rootNodeName + "/" + this.alliance;
            String phaseBarrierPath = barriersNodeName + phaseBarrierNodeName;
            String alliancePhaseBarrierPath = phaseBarrierPath + "/" + this.alliance;
            String deleteBarrierPath = barriersNodeName + deleteBarrierNodeName;
            String allianceDeleteBarrierPath = deleteBarrierPath + "/" + this.alliance;
            String backupBarrierPath = barriersNodeName + backupBarrierNodeName;
            String allianceBackupBarrierPath = backupBarrierPath + "/" + this.alliance;
            String balanceBarrierPath = barriersNodeName + balanceBarrierNodeName;
            String allianceBalanceBarrierPath = balanceBarrierPath + "/" + this.alliance;

            createNodeIfNotExists(alliancePath, getBytes(AllianceData.serialize(new AllianceData())));
            createNodeIfNotExists(phaseBarrierPath, new byte[0]);
            createNodeIfNotExists(alliancePhaseBarrierPath, new byte[0]);
            createNodeIfNotExists(deleteBarrierPath, new byte[0]);
            createNodeIfNotExists(allianceDeleteBarrierPath, new byte[0]);
            createNodeIfNotExists(backupBarrierPath, new byte[0]);
            createNodeIfNotExists(allianceBackupBarrierPath, new byte[0]);
            createNodeIfNotExists(balanceBarrierPath, new byte[0]);
            createNodeIfNotExists(allianceBalanceBarrierPath, new byte[0]);
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
            AllianceData allianceData = AllianceData.deserialize(getString(this.zk.GetData(allianceInRootNode, false, s)));
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
            List<String> serverNodesSortedList = serverNodes.ToList();
            serverNodesSortedList.Sort();

            Dictionary<String, String> nodeNameToAirline = new Dictionary<string, string>();
            Dictionary<String, String> airlineToNodeName = new Dictionary<string, string>();
            foreach (String serverNode in serverNodes)
            {
                String airlineInServerNode = allianceInServersNode + "/" + serverNode;
                Stat s = new Stat();
                String airline = ServerData.deserialize(getString(this.zk.GetData(airlineInServerNode, false, s))).airline;
                nodeNameToAirline[serverNode] = airline;
                airlineToNodeName[airline] = serverNode;
                
            }

            setLeader(allianceInServersNode, serverNodesSortedList);
            updatePhasePhase(allianceInServersNode, serverNodesSortedList, nodeNameToAirline, isIJoined);
            deleteOldDataPhase(allianceInServersNode, serverNodesSortedList, airlineToNodeName, isIJoined);
            backupPhase(allianceInServersNode, serverNodesSortedList, airlineToNodeName);
            balancePhase(allianceInServersNode, serverNodesSortedList, airlineToNodeName);
        }

        private void setLeader(String serversPath, IEnumerable<String> serverNodes)
        {
            Console.WriteLine("\n******************************\nStart Leader\n");

            List<string> serverNodesList = serverNodes.ToList();
            serverNodesList.Sort();

            if (serverNodesList[0].Equals(this.ephemeralNodeName))
            {
                Console.WriteLine("leader");
                if (!this.isLeader)
                {
                    Console.WriteLine("calling delegate");
                    this.leaderDelegate();
                }
                this.isLeader = true;
            }
            else
            {
                Console.WriteLine("not leader");
                this.isLeader = false;
            }
        }

        private void updatePhasePhase(String allianceInServersNode, IEnumerable<String> serverNodes, Dictionary<String, String> nodeNameToAirline, bool isIJoined)
        {
            Console.WriteLine("\n******************************\nStart Update Phase\n");

            String phaseBarrierPath = barriersNodeName + phaseBarrierNodeName + "/" + this.alliance;
            String phaseBarrierPathToAirline = phaseBarrierPath + "/" + this.ephemeralNodeName;
            
            DataChangedWatch dataChangedWatch = new DataChangedWatch(getAddress(), phaseBarrierPathToAirline);
            Barrier phaseBarrier = new Barrier(getAddress(), phaseBarrierPath, this.ephemeralNodeName, getBytes(this.phase.ToString()), serverNodes.Count());

            Console.WriteLine("sent phase - " + this.phase);

            Console.WriteLine("wait to enter barrier");
            phaseBarrier.Enter();
            Console.WriteLine("entered");

            if (this.isLeader)
            {
                IEnumerable<String> phaseBarrierChildren = this.zk.GetChildren(phaseBarrierPath, false);

                List<int> phases = new List<int>();
                foreach (String airline in phaseBarrierChildren)
                {
                    String airlineInBarrierNode = phaseBarrierPath + "/" + airline;
                    Stat s = new Stat();
                    phases.Add(Convert.ToInt32(getString(this.zk.GetData(airlineInBarrierNode, false, s))));
                }

                int maxPhase = phases.Max();

                Console.WriteLine("selected phase - " + maxPhase);
                
                Console.WriteLine("update zookeeper");
                foreach (String airline in phaseBarrierChildren)
                {
                    String airlineInBarrierNode = phaseBarrierPath + "/" + airline;
                    Stat s = new Stat();
                    this.zk.SetData(airlineInBarrierNode, getBytes(maxPhase.ToString()), -1);
                    Console.WriteLine("set max phase - " + maxPhase + " - to - " + nodeNameToAirline[airline]);
                }
            }

            Console.WriteLine("wait for phase update from leader");
            dataChangedWatch.Wait();

            Stat stat = new Stat();
            int newPhase = Convert.ToInt32(getString(this.zk.GetData(phaseBarrierPathToAirline, false, stat)));

            Console.WriteLine("received phase - " + newPhase);

            if (isIJoined)
            {
                setServers(newPhase, this.phases[this.phase]);
            }

            this.phase = newPhase;

            removeServers(newPhase);

            Console.WriteLine("calling delegate");
            this.updateDataToPhaseDelegate(newPhase);

            Console.WriteLine("wait to leave barrier");
            phaseBarrier.Leave();
            Console.WriteLine("left");

            //set zookeeper with new phase data
            if (this.isLeader)
            {
                Console.WriteLine("update zookeeper");
                foreach (String serverNode in serverNodes)
                {
                    String airlineInServerNode = allianceInServersNode + "/" + serverNode;
                    Stat s = new Stat();
                    ServerData serverData = ServerData.deserialize(getString(this.zk.GetData(airlineInServerNode, false, s)));
                    foreach (ServerData sd in this.phases[this.phase])
                    {
                        if (serverData.airline.Equals(sd.airline))
                        {
                            this.zk.SetData(airlineInServerNode, getBytes(ServerData.serialize(sd)), -1);
                            Console.WriteLine("update node - " + serverNode + " - airline - " + nodeNameToAirline[serverNode]);
                            break;
                        }
                    }
                }            
            }
        }

        private void deleteOldDataPhase(String allianceInServersNode, IEnumerable<String> serverNodes, Dictionary<String, String> airlineToNodeName, bool isIJoined)
        {
            Console.WriteLine("\n******************************\nStart Delete Old Data\n");

            String deleteBarrierPath = barriersNodeName + deleteBarrierNodeName + "/" + this.alliance;
            Barrier deleteBarrier = new Barrier(getAddress(), deleteBarrierPath, this.ephemeralNodeName, new byte[0], serverNodes.Count());

            if (isIJoined)
            {
                Console.WriteLine("joined - nothing to do here");
                deleteBarrier.Enter();
                deleteBarrier.Leave();
                return;
            }

            Console.WriteLine("wait to enter barrier");
            deleteBarrier.Enter();
            Console.WriteLine("entered");

            String action = null;
            String airlineChanged = null;

            List<String> prevNodes = new List<string>();
            foreach (ServerData serverData in this.phases[this.phase])
            {
                prevNodes.Add(serverData.airline);
            }

            List<String> currNodes = new List<string>();
            List<ServerData> serversData = new List<ServerData>();
            foreach (String airline in serverNodes)
            {
                String airlineInAllianceNode = allianceInServersNode + "/" + airline;
                Stat s = new Stat();
                ServerData sd = ServerData.deserialize(getString(this.zk.GetData(airlineInAllianceNode, false, s)));
                currNodes.Add(sd.airline);
                serversData.Add(sd);
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

            List<ServerData> serversDataAfterDeletion = null;
            if (action.Equals(NODE_JOINED))
            {
                Console.WriteLine("calling delegate");
                serversDataAfterDeletion = this.deleteOldDataDelegate(serversData, airlineChanged);
                setServers(this.phase, serversDataAfterDeletion);
            }

            Console.WriteLine("wait to leave barrier");
            deleteBarrier.Leave();
            Console.WriteLine("left");

            if (action.Equals(NODE_JOINED) && this.isLeader)
            {
                Console.WriteLine("update zookeeper");
                foreach (ServerData serverData in serversDataAfterDeletion)
                {
                    this.zk.SetData(allianceInServersNode + "/" + airlineToNodeName[serverData.airline], getBytes(ServerData.serialize(serverData)), -1);
                    Console.WriteLine("update node - " + airlineToNodeName[serverData.airline] + " - airline - " + serverData.airline);
                }
            }
        }

        private void backupPhase(String allianceInServersNode, IEnumerable<String> serverNodes, Dictionary<String, String> airlineToNodeName)
        {
            Console.WriteLine("\n******************************\nStart Backup\n");

            String backupBarrierPath = barriersNodeName + backupBarrierNodeName + "/" + this.alliance;
            Barrier backupBarrier = new Barrier(getAddress(), backupBarrierPath, this.ephemeralNodeName, new byte[0], serverNodes.Count());

            Console.WriteLine("wait to enter barrier");
            backupBarrier.Enter();
            Console.WriteLine("entered");

            List<ServerData> serversData = new List<ServerData>();
            foreach (String airline in serverNodes)
            {
                String airlineInAllianceNode = allianceInServersNode + "/" + airline;
                Stat s = new Stat();
                ServerData sd = ServerData.deserialize(getString(this.zk.GetData(airlineInAllianceNode, false, s)));
                serversData.Add(sd);
            }

            Console.WriteLine("calling delegate");
            List<ServerData> serversDataAfterBackup = this.backupDelegate(serversData);
            setServers(this.phase, serversDataAfterBackup);

            Console.WriteLine("wait to leave barrier");
            backupBarrier.Leave();
            Console.WriteLine("left");

            if (this.isLeader)
            {
                Console.WriteLine("update zookeeper");
                foreach (ServerData serverData in serversDataAfterBackup)
                {
                    this.zk.SetData(allianceInServersNode + "/" + airlineToNodeName[serverData.airline], getBytes(ServerData.serialize(serverData)), -1);
                    Console.WriteLine("update node - " + airlineToNodeName[serverData.airline] + " - airline - " + serverData.airline);
                }
            }
        }

        private void balancePhase(String allianceInServersNode, IEnumerable<String> serverNodes, Dictionary<String, String> airlineToNodeName)
        {
            Console.WriteLine("\n******************************\nStart Balance\n");

            String balanceBarrierPath = barriersNodeName + balanceBarrierNodeName + "/" + this.alliance;
            Barrier balanceBarrier = new Barrier(getAddress(), balanceBarrierPath, this.ephemeralNodeName, new byte[0], serverNodes.Count());

            Console.WriteLine("wait to enter barrier");
            balanceBarrier.Enter();
            Console.WriteLine("entered");

            List<ServerData> serversData = new List<ServerData>();
            foreach (String airline in serverNodes)
            {
                String airlineInAllianceNode = allianceInServersNode + "/" + airline;
                Stat s = new Stat();
                ServerData sd = ServerData.deserialize(getString(this.zk.GetData(airlineInAllianceNode, false, s)));
                serversData.Add(sd);
            }

            Stat stat = new Stat();
            AllianceData allianceData = AllianceData.deserialize(getString(this.zk.GetData(allianceInServersNode, false, stat)));
            allianceData.airlines.Sort();

            Console.WriteLine("calling delegate");
            List<ServerData> serversDataAfterBalance = this.balanceDelegate(serversData, allianceData.airlines, this.phase + 1);
            setServers(this.phase + 1, serversDataAfterBalance);

            Console.WriteLine("wait to leave barrier");
            balanceBarrier.Leave();
            Console.WriteLine("left");

            this.phase++;
            removeServers(this.phase);
            Console.WriteLine("moved to new phase");
            
            if (this.isLeader)
            {
                Console.WriteLine("update zookeeper");
                foreach (ServerData serverData in serversDataAfterBalance)
                {
                    this.zk.SetData(allianceInServersNode + "/" + airlineToNodeName[serverData.airline], getBytes(ServerData.serialize(serverData)), -1);
                    Console.WriteLine("update node - " + airlineToNodeName[serverData.airline] + " - airline - " + serverData.airline);
                }
            }
        }
            
        private void createNodeIfNotExists(String path, byte[] data)
        {
            if (this.zk.Exists(path, false) == null)
            {
                this.zk.Create(path, data, Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }
        }
        
        private void setServers(int currPhase, List<ServerData> servers)
        {
            this.phases[currPhase] = servers;        
        }

        private void removeServers(int currPhase)
        {
            List<int> keys = this.phases.Keys.ToList();
            foreach (int key in keys)
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
