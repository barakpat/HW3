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
            Distributer d = new Distributer(args[0], args[1], args[2]);
            d.join();
            Console.ReadKey();
        }
    }
}
