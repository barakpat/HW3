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
            get { return "http://" + IP + ":" + alliancePort + "/allience"; }
        }

        public String airline { get; set; }
        public String alliance { get; set; }
        public String searchPort { get; set; }
        public String alliancePort { get; set; }
        public Distributer distributer { get; set; }
        public AirlineCommunication airlineCommunicationServer { get; set; } 

        WebChannelFactory<ISellerService> proxy;
        ISellerService channel;
        public AirlineServerSoap(string[] args, AirlineCommunication airlineCommunicationServer)
        {
            this.airlineCommunicationServer = airlineCommunicationServer;
            this.airline = args[0];
            this.alliance = args[1];
            this.searchPort = args[2];
            this.alliancePort = args[3];
            this.proxy = new WebChannelFactory<ISellerService>(new Uri(args[4]));
            this.channel = this.proxy.CreateChannel();
            this.airlineCommunicationServer.distributer = new Distributer(this.alliance, this.airline, this.AllienceUri);
            //distributer = new Distributer(this.alliance, this.airline, this.AllienceUri);
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
            return this.airlineCommunicationServer.distributer.isDelegate();
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

        
        /*
        public Flights SearchSrcAndDst(String src, String dst, DateTime date)
        {
            Flights resFlights = new Flights();

            foreach (ServerFlight f in flights)
            {
                if (f.Src == src &&  f.Dst == dst && f.Date == date)
                {
                    resFlights.Add(new Flight(f));
                }

            }
            return resFlights;
        }

        public Flights SearchToDstNotFromSrc(String src, String dst, DateTime date)
        {
            Flights resFlights = new Flights();

            foreach (ServerFlight f in flights)
            {
                if (f.Src != src &&  f.Dst == dst && f.Date == date)
                {
                    resFlights.Add(new Flight(f));
                }

            }
            return resFlights;
        }

        public Flights SearchFromSrcNotToDst(String src,String dst, DateTime date)
        {
            Flights resFlights = new Flights();

            foreach (ServerFlight f in flights)
            {
                if (f.Src == src && f.Dst != dst && f.Date == date)
                {
                    resFlights.Add(new Flight(f));
                }

            }
            return resFlights;
        }
        */


        public ConnectionFlights Search(String src, String dst, DateTime date, String airline)
        {
            
            /*
            Flights srcDstFlights = new Flights();
            Flights srcFlights = new Flights();
            Flights dstFlightsDay1 = new Flights();
            Flights dstFlightsDay2 = new Flights();
            
            //create connection to other servers - TODO should be outside the function
            

            ConnectionFlights resFlights = new ConnectionFlights();
            
           
            Flights srcDstFlightsDay1 = this.SearchSrcAndDst(src, dst, date);
          //  Flights srcDstFlightsDay2 = this.SearchSrcAndDst(src, dst, date.AddDays(1));

            Flights srcFlightsDay1 = this.SearchFromSrcNotToDst(src, dst, date);
            //Flights srcFlightsDay2 = this.SearchFromSrc(src, date.AddDays(1));
            dstFlightsDay1 = this.SearchToDstNotFromSrc(src,  dst, date);
            dstFlightsDay2 = this.SearchToDstNotFromSrc(src,  dst, date.AddDays(1));

            foreach (Flight f in srcDstFlightsDay1)
            {
                    resFlights.Add(new ConnectionFlight(f));
            }

            foreach (Flight f1 in srcFlightsDay1)
            {
                foreach (Flight f2 in dstFlightsDay1)
                {
                    if (f1.Dst == f2.Src)
                    {
                         resFlights.Add(new ConnectionFlight(f1,f2));
                    }
                }
                foreach (Flight f2 in dstFlightsDay2)
                {
                    if (f1.Dst == f2.Src)
                    {
                        resFlights.Add(new ConnectionFlight(f1, f2));
                    }
                }
            }

            Console.WriteLine("Search: " + "src: " + src + " " + "dst: " + dst + " " + "date: " + date + " ");

            return resFlights;
            */
            return this.airlineCommunicationServer.SearchAllServers(src, dst, date, airline);
            
        }

        

        public void joinCluster()
        {
            this.airlineCommunicationServer.distributer.join();
        }

        public void registerDelegate(string URI)
        {
            AllianceDelegate allianceDelegate = new AllianceDelegate();
            allianceDelegate.isDelegate = true; //this.distributer.isDelegate();
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
