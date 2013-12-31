using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HW3_Zookeeper
{
    class Program
    {
        static void Main(string[] args)
        {
            HW3_Zookeeper.Distributer.LeaderDelegate leaderDelegate = leader;
            HW3_Zookeeper.Distributer.UpdateDataToPhaseDelegate updateDataToPhaseDelegate = updateDataToPhase;
            HW3_Zookeeper.Distributer.DeleteOldDataDelegate deleteOldDataDelegate = deleteOldData;
            HW3_Zookeeper.Distributer.BackupDelegate backupDelegate = backup;
            HW3_Zookeeper.Distributer.BalanceDelegate balanceDelegate = balance;
            Distributer d = new Distributer(args[0], args[1], args[2], leaderDelegate, updateDataToPhaseDelegate, deleteOldDataDelegate, backupDelegate, balanceDelegate);
            d.join();
            Console.ReadKey();
            d.leave();
            d.Dispose();
        }
        
        public static void leader()
        {
            Console.WriteLine("leader choosen");
        }
        
        public static void updateDataToPhase(int phase)
        {
            Console.WriteLine("new phase: " + phase);
        }

        public static List<ServerData> deleteOldData(List<ServerData> serversData, String deletedAirline)
        {
            Console.WriteLine("delete old data delegate - joined airline: " + deletedAirline);
            return serversData;
        }

        public static List<ServerData> backup(List<ServerData> serversData)
        {
            Console.WriteLine("backup delegate");
            return serversData;
        }

        public static List<ServerData> balance(List<ServerData> servers, List<String> airlines, int newPhase)
        {
            Console.WriteLine("balance delegate");
            return servers;
        }
    }
}
