using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace AirlineServer
{
    class Program
    {
        static void Main(string[] args)
        {   

            try
            {
                AirlineServerSoap airlineServer = new AirlineServerSoap(args);
                using (ServiceHost host = new ServiceHost(
                    airlineServer, new Uri("http://localhost:" + airlineServer.searchPort + "/services")))
                {
                    var behaviour = host.Description.Behaviors.Find<ServiceBehaviorAttribute>();
                    behaviour.InstanceContextMode = InstanceContextMode.Single;
                    // define the service endpoints
                    host.Open();

                    // TODO -join cluster
                    airlineServer.joinCluster();
                    airlineServer.registerDelegate("http://" + LocalIPAddress() + ":" + airlineServer.searchPort + "/services");
                    

                    // old- hw1
                    //airlineServer.registerSeller("http://" + LocalIPAddress() + ":" + args[0] + "/services");

                    Console.WriteLine("Airline server,' " + airlineServer.airline + "', is up and running listenning to port " + airlineServer.searchPort);
                    Console.ReadKey();

                   //TODO - leave cluster

                    // old - hw1
                    // airlineServer.unregisterFromServer();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Server has crushed, probably the parameters are wrong :(");
                Console.WriteLine(e);
            }

        }

        static public string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }
    }
}
