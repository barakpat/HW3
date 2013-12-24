using HW1c;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace AirlineServer
{
    class AirlineServerSoap : IAirlineServerSoap
    {
        public class ServerFlight : Flight
        {
            public int Seats { get; set; }
        }

        // in-memory resource collections
        List<ServerFlight> flights = new List<ServerFlight>();
        Dictionary<int, String> reservations = new Dictionary<int, String>();
        int reservationId = 0;
        String seller;
        String port;
        WebChannelFactory<ISellerService> proxy;
        ISellerService channel;
        public AirlineServerSoap(string[] args)
        {
            this.seller = args[3];
            this.port = args[0];
            this.proxy = new WebChannelFactory<ISellerService>(new Uri(args[1]));
            this.channel = this.proxy.CreateChannel();
            this.initializeFlights(args[2]);
           // this.registerSeller(args);
        }


        public void unregisterFromServer(){
            using (
                    new OperationContextScope((IContextChannel)this.channel))
            {
                bool r = this.channel.unregisterServer(this.seller);
            }
            Console.WriteLine("unregister from search server");
        }

        public bool isDelegate() {
            return true;
        }

        public void registerSeller(string URI)
        {

            TicketServer newTs = new TicketServer();
            newTs.ServerName = this.seller;
            newTs.Port = this.port;
            newTs.ServiceURI = new Uri(URI + "/TicketSellingServerSoap");
            using (
                    new OperationContextScope((IContextChannel)this.channel))
            { 
                bool r = this.channel.registerServer(newTs);
            }
            Console.WriteLine("register to search server");
            
        }

        

        // read flights from file
        private void initializeFlights(String fileName)
        {
            String line;
            System.IO.StreamReader file =
                 new System.IO.StreamReader(fileName);
            ServerFlight f;
            while ((line = file.ReadLine()) != null)
            {
                String[] words = line.Split();
                f = new ServerFlight();
                f.Seller = this.seller;
                f.FNum = words[0];
                f.Src = words[1];
                f.Dst = words[2];
                f.Date = DateTime.ParseExact(words[3], "dd/MM/yyyy", CultureInfo.InvariantCulture);
                f.AvailableSeats = f.Seats = Convert.ToInt32(words[4]);
                f.Price = Convert.ToInt32(words[5]);
                this.flights.Add(f);
            }

            file.Close();
        }

        public Flights Search(String src, String dst, DateTime date)
        {
            Flights resFlights = new Flights();
            
            foreach(ServerFlight f in flights ){
                if (f.Src == src && f.Dst == dst && f.Date == date)
                {
                    resFlights.Add(new Flight(f));
                }

            }            
            Console.WriteLine("Search: " + "src: " + src + " " + "dst: " + dst + " " + "date: " + date + " ");
            return resFlights;
        }

        public void joinCluster()
        {
            //throw new NotImplementedException();
        }

        public void registerDelegate(string p)
        {
            //throw new NotImplementedException();
        }
    }
}
