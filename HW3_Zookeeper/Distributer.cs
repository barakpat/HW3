using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZooKeeperNet;
using Org.Apache.Zookeeper.Data;
using System.Threading;

//TODO: check when event occures when in balance mode
//TODO: clean the nodes in barriers when exiting balance mode

namespace HW3_Zookeeper
{

    public class Distributer : IDistributer, IWatcher, IDisposable
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
        private Thread algorithmWorker;
        private bool inGracePeriod = false;
        private readonly Object gracePeriodMutex = new Object();
        private bool inBalanceMode = false;
        private readonly Object balanceMutex = new Object();
        private bool isTerminated = false;
        private readonly Object terminationMutex = new Object();

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
            lock (this.terminationMutex)
            {
                if (this.isTerminated)
                {
                    return;
                }
            }
            lock (this.gracePeriodMutex)
            {
                if (this.inGracePeriod)
                {
                    Console.WriteLine("\n*****************************");
                    Console.WriteLine("*****************************");
                    Console.WriteLine("*****************************");
                    Console.WriteLine("** ERROR - IN GRACE PERIOD **");
                    Console.WriteLine("*****************************");
                    Console.WriteLine("*****************************");
                    Console.WriteLine("*****************************\n");
                }
            }

            if (@event.State == KeeperState.SyncConnected && @event.Type == EventType.None)
            {
                this.connectedSignal.Set();
            }
            if (@event.Type == EventType.NodeChildrenChanged)
            {
                runAlgorithmWorkerThread(false);
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

            runAlgorithmWorkerThread(true);
        }

        private void runAlgorithmWorkerThread(bool isIJoined)
        {
            lock (this.balanceMutex)
            {
                if (this.inBalanceMode)
                {
                    this.algorithmWorker.Abort();
                    try
                    {
                        this.zk.Delete(barriersNodeName + balanceBarrierNodeName + "/" + this.alliance + "/" + this.ephemeralNodeName, -1);
                    }
                    catch (Exception){}
                }
            }
            AlgorithmWorker algorithmWorkerThread = new AlgorithmWorker(this, isIJoined);
            this.algorithmWorker = new Thread(new ThreadStart(algorithmWorkerThread.Run));
            this.algorithmWorker.Start();        
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

        public void Dispose()
        {
            lock (this.terminationMutex)
            {
                this.isTerminated = true;
            }
            this.zk.Dispose();
        }

        private class AlgorithmWorker
        {    
            Distributer parent;
            bool isIJoined;

            public AlgorithmWorker(Distributer parent, bool isIJoined)
            {
                this.parent = parent;
                this.isIJoined = isIJoined;
            }
            
            public void Run(){
                this.parent.algorithm(this.isIJoined);
            }
        }

        public void algorithm(bool isIJoined)
        {
            lock (this.gracePeriodMutex)
            {
                this.inGracePeriod = true;
            }
            lock (this.balanceMutex)
            {
                this.inBalanceMode = false;
            }

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
            deleteOldDataPhase(allianceInServersNode, serverNodesSortedList, nodeNameToAirline, airlineToNodeName, isIJoined);
            backupPhase(allianceInServersNode, serverNodesSortedList, nodeNameToAirline, airlineToNodeName);

            lock (this.gracePeriodMutex)
            {
                this.inGracePeriod = false;
            }
            lock (this.balanceMutex)
            {
                this.inBalanceMode = true;
            }

            balancePhase(allianceInServersNode, serverNodesSortedList, nodeNameToAirline, airlineToNodeName);

            lock (this.balanceMutex)
            {
                this.inBalanceMode = false;
            }
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

            DataChangedWatch dataChangedWatch = new DataChangedWatch(this.zk, phaseBarrierPathToAirline);
            Barrier phaseBarrier = new Barrier(this.zk, phaseBarrierPath, this.ephemeralNodeName, getBytes(this.phase.ToString()), serverNodes.Count(), this.isLeader);

            Console.WriteLine("sent phase - " + this.phase);

            Console.WriteLine("wait to enter barrier");
            phaseBarrier.Enter();

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
                
                foreach (String airline in phaseBarrierChildren)
                {
                    String airlineInBarrierNode = phaseBarrierPath + "/" + airline;
                    Stat s = new Stat();
                    this.zk.SetData(airlineInBarrierNode, getBytes(maxPhase.ToString()), -1);
                    Console.WriteLine("sent max phase - " + maxPhase + " - to - " + nodeNameToAirline[airline]);
                }
            }

            dataChangedWatch.Wait();
            Console.WriteLine("entered");

            Stat stat = new Stat();
            int newPhase = Convert.ToInt32(getString(this.zk.GetData(phaseBarrierPathToAirline, false, stat)));

            Console.WriteLine("received phase - " + newPhase);

            if (isIJoined)
            {
                setServers(newPhase, this.phases[this.phase]);
            }

            updateDataToNewPhase(newPhase);

            Console.WriteLine("wait to leave barrier");
            phaseBarrier.Leave();
            Console.WriteLine("left");

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
                            Console.WriteLine("update " + nodeNameToAirline[serverNode]);
                            break;
                        }
                    }
                }            
            }
        }

        private void deleteOldDataPhase(String allianceInServersNode, IEnumerable<String> serverNodes, Dictionary<String, String> nodeNameToAirline, Dictionary<String, String> airlineToNodeName, bool isIJoined)
        {
            Console.WriteLine("\n******************************\nStart Delete Old Data\n");

            String deleteBarrierPath = barriersNodeName + deleteBarrierNodeName + "/" + this.alliance;
            String deleteBarrierPathToAirline = deleteBarrierPath + "/" + this.ephemeralNodeName;

            DataChangedWatch dataChangedWatch = new DataChangedWatch(this.zk, deleteBarrierPathToAirline);
            Barrier deleteBarrier = new Barrier(this.zk, deleteBarrierPath, this.ephemeralNodeName, new byte[0], serverNodes.Count(), this.isLeader);

            Console.WriteLine("wait to enter barrier");
            deleteBarrier.Enter();

            if (this.isLeader)
            {
                IEnumerable<String> deleteBarrierChildren = this.zk.GetChildren(deleteBarrierPath, false);

                foreach (String airline in deleteBarrierChildren)
                {
                    String airlineInBarrierNode = deleteBarrierPath + "/" + airline;
                    Stat s = new Stat();
                    this.zk.SetData(airlineInBarrierNode, getBytes(""), -1);
                    Console.WriteLine("touch " + nodeNameToAirline[airline]);
                }
            }
            
            dataChangedWatch.Wait();
            Console.WriteLine("entered");

            if (isIJoined)
            {
                Console.WriteLine("joined - nothing to do here");
                deleteBarrier.Leave();
                return;
            }

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
                    Console.WriteLine("update " + serverData.airline);
                }
            }
        }

        private void backupPhase(String allianceInServersNode, IEnumerable<String> serverNodes, Dictionary<String, String> nodeNameToAirline, Dictionary<String, String> airlineToNodeName)
        {
            Console.WriteLine("\n******************************\nStart Backup\n");

            String backupBarrierPath = barriersNodeName + backupBarrierNodeName + "/" + this.alliance;
            String backupBarrierPathToAirline = backupBarrierPath + "/" + this.ephemeralNodeName;

            DataChangedWatch dataChangedWatch = new DataChangedWatch(this.zk, backupBarrierPathToAirline);
            Barrier backupBarrier = new Barrier(this.zk, backupBarrierPath, this.ephemeralNodeName, new byte[0], serverNodes.Count(), this.isLeader);

            Console.WriteLine("wait to enter barrier");
            backupBarrier.Enter();

            if (this.isLeader)
            {
                IEnumerable<String> backupBarrierChildren = this.zk.GetChildren(backupBarrierPath, false);

                foreach (String airline in backupBarrierChildren)
                {
                    String airlineInBarrierNode = backupBarrierPath + "/" + airline;
                    Stat s = new Stat();
                    this.zk.SetData(airlineInBarrierNode, getBytes(""), -1);
                    Console.WriteLine("touch " + nodeNameToAirline[airline]);
                }
            }

            dataChangedWatch.Wait();
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
                    Console.WriteLine("update " + serverData.airline);
                }
            }
        }

        private void balancePhase(String allianceInServersNode, IEnumerable<String> serverNodes, Dictionary<String, String> nodeNameToAirline, Dictionary<String, String> airlineToNodeName)
        {
            Console.WriteLine("\n******************************\nStart Balance\n");
            
            String balanceBarrierPath = barriersNodeName + balanceBarrierNodeName + "/" + this.alliance;
            String balanceBarrierPathToAirline = balanceBarrierPath + "/" + this.ephemeralNodeName;

            DataChangedWatch dataChangedWatch = new DataChangedWatch(this.zk, balanceBarrierPathToAirline);
            Barrier balanceBarrier = new Barrier(this.zk, balanceBarrierPath, this.ephemeralNodeName, new byte[0], serverNodes.Count(), this.isLeader);

            Console.WriteLine("wait to enter barrier");
            balanceBarrier.Enter();

            if (this.isLeader)
            {
                IEnumerable<String> balanceBarrierChildren = this.zk.GetChildren(balanceBarrierPath, false);

                foreach (String airline in balanceBarrierChildren)
                {
                    String airlineInBarrierNode = balanceBarrierPath + "/" + airline;
                    Stat s = new Stat();
                    this.zk.SetData(airlineInBarrierNode, getBytes(""), -1);
                    Console.WriteLine("touch " + nodeNameToAirline[airline]);
                }
            }

            dataChangedWatch.Wait();
            Console.WriteLine("entered");

//            System.Threading.Thread.Sleep(2000);

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

            int newPhase = this.phase + 1;
            
            Console.WriteLine("calling delegate");
            List<ServerData> serversDataAfterBalance = this.balanceDelegate(serversData, allianceData.airlines, newPhase);
            setServers(newPhase, serversDataAfterBalance);

            Console.WriteLine("wait to leave barrier");
            balanceBarrier.Leave();
            Console.WriteLine("left");

            updateDataToNewPhase(newPhase);
            
            if (this.isLeader)
            {
                Console.WriteLine("update zookeeper");
                foreach (ServerData serverData in serversDataAfterBalance)
                {
                    this.zk.SetData(allianceInServersNode + "/" + airlineToNodeName[serverData.airline], getBytes(ServerData.serialize(serverData)), -1);
                    Console.WriteLine("update " + serverData.airline);
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

        private void updateDataToNewPhase(int newPhase)
        {
            this.phase = newPhase;
            removeServers(newPhase);
            Console.WriteLine("moved to new phase");

            Console.WriteLine("calling delegate");
            this.updateDataToPhaseDelegate(newPhase);
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
