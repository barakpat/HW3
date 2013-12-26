using HW1c;
using HW3_Zookeeper;
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
        //int reservationId = 0;

        public String IP
        {
            get { return Program.LocalIPAddress(); }
        }
        public String SearchUri
        {
            get { return "http://" + IP + ":" + searchPort + "/services"; }
        }
        public String AllienceUri
        {
            get { return "http://" + IP + ":" + alliancePort + "/services"; }
        }

        public String airline { get; set; }
        public String alliance { get; set; }
        public String searchPort { get; set; }
        public String alliancePort { get; set; }
        public Distributer distributer { get; set; }
        WebChannelFactory<ISellerService> proxy;
        ISellerService channel;
        public AirlineServerSoap(string[] args)
        {
            this.airline = args[0];
            this.alliance = args[1];
            this.searchPort = args[2];
            this.alliancePort = args[3];
            this.proxy = new WebChannelFactory<ISellerService>(new Uri(args[4]));
            this.channel = this.proxy.CreateChannel();
            this.initializeFlights(args[5]);

            distributer = new Distributer(this.alliance, this.airline, this.AllienceUri);
           // this.registerSeller(args);
        }

        

        public void unregisterFromServer(){
            using (
                    new OperationContextScope((IContextChannel)this.channel))
            {
                bool r = this.channel.unregisterServer(this.airline);
            }
            Console.WriteLine("unregister from search server");
        }

        public bool isDelegate() {
            return this.distributer.isDelegate();
        }

        public void registerSeller(string URI)
        {

            AllianceDelegate newTs = new AllianceDelegate();
            newTs.AllianceName = this.airline;
            newTs.Port = this.searchPort;
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
                f.Seller = this.airline;
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
            this.distributer.join();
        }

        public void registerDelegate(string URI)
        {
            AllianceDelegate allianceDelegate = new AllianceDelegate();
            allianceDelegate.isDelegate = this.distributer.isDelegate();
            allianceDelegate.AirlineName = this.airline;
            allianceDelegate.AllianceName = this.alliance;
            allianceDelegate.Port = this.searchPort;
            allianceDelegate.ServiceURI = new Uri(URI + "/AirlineServerSoap");
            using (
                    new OperationContextScope((IContextChannel)this.channel))
            {
                bool r = this.channel.registerServer(allianceDelegate);
            }
            Console.WriteLine("register to search server");
        }
    }
}
