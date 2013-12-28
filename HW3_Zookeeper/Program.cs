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
            HW3_Zookeeper.Distributer.UpdateDataToPhaseDelegate updateDataToPhaseDelegate = updateDataToPhase;
            HW3_Zookeeper.Distributer.DeleteOldDataDelegate deleteOldDataDelegate = deleteOldData;
            Distributer d = new Distributer(args[0], args[1], args[2], updateDataToPhaseDelegate, deleteOldDataDelegate);
            d.join();
            Console.ReadKey();
            d.leave();
            Console.ReadKey();
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
    }
}
