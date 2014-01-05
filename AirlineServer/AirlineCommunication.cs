﻿using HW1c;
using HW3_Zookeeper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace AirlineServer
{
    class AirlineCommunication : IAirlineCommunication
    {

        readonly static int initialVersion = -1;
        readonly static int minimalNumberOfCopies = 2;
        int currentVersion = initialVersion;

        // Dictionary of channel to the servers 
        Dictionary<String, IAirlineCommunication> serversChannels = new Dictionary<string, IAirlineCommunication>();
     
        
        // AirlineFlightsData airlineFlightsData = new AirlineFlightsData();
        //AirlinesFlightsData airlinesFlightsData = new AirlinesFlightsData();
        
        // Sever data with versioning 
        //TODO this dictionary must be concurent!!!!!!!!!!!!!!
        Dictionary<int, AirlinesFlightsData> serverData = new Dictionary<int, AirlinesFlightsData>();
        
       // Dictionary<string, List<Flight> > airlineFlights = new Dictionary<string, List<Flight> >();
       // List<String> serverAirlines = new List<String>();
        String serverName;
        
        public Distributer distributer { get; set; }
        public Cache cache{ get; set; }

        public AirlineCommunication(string[] args) 
        {
            this.serverName = args[0];
           // this.serverAirlines.Add(this.serverName);
            this.initializeFlights(args[5]);

        }

        // replication algorithm delegate method
        public void updatePhase(int p)
        {
            if (this.currentVersion == AirlineCommunication.initialVersion)
            {
                AirlinesFlightsData tmpAirlinesFlightsData = getCurrentPhaseDate();
                this.serverData.Add(p, tmpAirlinesFlightsData);
                this.serverData.Remove(this.currentVersion);
                this.currentVersion = p;
                return;
            }
            else if (this.serverData.Keys.Contains(p))
            {
                this.currentVersion = p;
                
                foreach (int key in new List<int>(this.serverData.Keys))
                {
                    if (key != p) this.serverData.Remove(key);
                }
                this.printData(null, "******** Update Phase ********");
                return;
            }
            else
            {

            }

            Console.WriteLine("The phase is not in the data , Something is wronge");
        }

        // replication algorithm delegate method
        public List<ServerData> deleteOldData(List<ServerData> servers, String airline)
        {
            this.printData(servers, "******** DELETE OLD DATA  - BEGIN ********");
            
            


            AirlinesFlightsData airlinesFlightsData = getCurrentPhaseDate();
            airlinesFlightsData.Remove(airline);

            foreach (ServerData server in servers) 
            {
                if (server.airline != airline) {
                    server.airlines = server.airlines.Where(x => x.name != airline).ToList();
                }
            }

            this.printData(servers, "******** DELETE OLD DATA  - END ********");
           

            return servers;
        }

        private AirlinesFlightsData getCurrentPhaseDate()
        {
            return this.getCurrentPhaseDate(this.currentVersion);
        }
        
        
        private AirlinesFlightsData getCurrentPhaseDate(int phase)
        {
            AirlinesFlightsData tmpAirlinesFlightsData = new AirlinesFlightsData();
            this.serverData.TryGetValue(phase, out tmpAirlinesFlightsData);
            if (tmpAirlinesFlightsData == null)
            {
                tmpAirlinesFlightsData = new AirlinesFlightsData();
                this.serverData.Add(phase, tmpAirlinesFlightsData);
            }
            return tmpAirlinesFlightsData;
        }

        // replication algorithm delegate method
        public List<ServerData> backUp(List<ServerData> allienceServers)
        {
            //delete cache
            this.cache.clearCache();
            
            this.printData(allienceServers, " ************       Backup - begin  ******");
            
            
            //
            this.updateChannelsList(allienceServers);
            

            AirlinesFlightsData tmpAirlinesFlightsData = getCurrentPhaseDate();
            ServerData s = allienceServers.Find(server => server.airline == this.serverName);
            //s.airlines.Select(airline => airline.name);
            //List<String> myAirline = new List<string>(tmpAirlinesFlightsData.Keys);
            List<String> myAirline = new List<string>(s.airlines.Select(airline => airline.name));
            foreach (String airline in myAirline)
            {
                if (!isBackedUp(airline, allienceServers))
                {
                    // fetching the airline flight data
                    AirlineFlightsData airlineFlightsData = new AirlineFlightsData();
                    tmpAirlinesFlightsData.TryGetValue(airline, out airlineFlightsData);
                    
                    // makign this copy to be the primary
                    airlineFlightsData.backup = false;

                    // if there is only 1 server running there is no backing up
                    if (allienceServers.Count() == 1) continue;
                    
                    // more than 1 server so we are backing up
                    backupAirline(airlineFlightsData, allienceServers);
                }
            }
            List<ServerData> newAllienceServers = calcNewAllienceServersState(allienceServers);

            this.printData(allienceServers, "********Backup - END ********");
            
            return newAllienceServers;
        }

        private void printData(List<ServerData> allienceServers, String comment)
        {
            //Console.WriteLine();
            //Console.WriteLine();            
            //Console.WriteLine();
            //Console.WriteLine(comment);
            //Console.WriteLine(comment);
            //Console.WriteLine(comment);
            //Console.WriteLine(" ****  Boris DATA ******");
            //if (allienceServers != null)
            //{
            //    foreach (ServerData s in allienceServers)
            //    {
            //        Console.Write(s.airline + " - ");
            //        ;
            //        foreach (AirlineData a in s.airlines)
            //        {
            //            Console.Write(a.name + ", ");
            //        }
            //        Console.WriteLine();
            //    }
            //}

            //Console.WriteLine(" ****  BARAK DATA ******");
            //Console.Write(this.serverName + " - ");
            //AirlinesFlightsData tmpAirlinesFlightsData = getCurrentPhaseDate();
            //List<String> myAirline = new List<string>(tmpAirlinesFlightsData.Keys);
            //foreach (String airline in myAirline)
            //{
            //    Console.Write(airline + ", ");
            //}
            //Console.WriteLine();
        }

        private void updateChannelsList(List<ServerData> allienceServers)
        {
            foreach (ServerData server in allienceServers){
                if (!this.serversChannels.ContainsKey(server.airline) && server.airline != this.serverName)
                {
                    this.serversChannels.Add(server.airline, createChannelForServer(server));
                }
            }

            // Delete the not relevant connection in case of a failure
            List<String> myServerChannelNames = new List<string>(this.serversChannels.Keys);
            foreach (String serverName in myServerChannelNames)
            {
                if (!allienceServers.Exists(server => server.airline == serverName)){
                    //IAirlineCommunication c;
                    //this.serversChannels.TryGetValue(serverName, out c);
                    //c.
                    this.serversChannels.Remove(serverName);
                }
            }
        }

        /**
         * Calculate the new allience state of the new servers
         */
        private List<ServerData> calcNewAllienceServersState(List<ServerData> allienceServers)
        {
            List<ServerData> tmpAllienceServers = new List<ServerData>(allienceServers.ToList());
            foreach (ServerData server in tmpAllienceServers)
            {
                foreach (AirlineData a in server.airlines)
                {
                    if (!isBackedUp(a.name, allienceServers))
                    {
                        // find the original server in the original list from the distributer
                        ServerData originalServer = allienceServers.Find(s => s.airline == server.airline);
                        AirlineData originalAirline = originalServer.airlines.Find(airline => airline.name == a.name);

                        // set node to primary
                        originalAirline.isPrimary = true;

                        if (allienceServers.Count() == 1) continue;

                        //gettign the traget server from the real list and update the list 
                        ServerData targetServer = findTargetServer(a.name, allienceServers);
                        AirlineData backupAirlineData = new AirlineData(a.name, !a.isPrimary);
                        targetServer.airlines.Add(backupAirlineData);
                    }
                    
                }
            }
            return allienceServers;
        }


        /*
         * backing up a flight data in a new airline 
         * */
        private void backupAirline(AirlineFlightsData airlineFlightsData, List<ServerData> allienceServers)
        {
            ServerData targetServer = findTargetServer(airlineFlightsData.airlineName, allienceServers);
            AirlineFlightsData copyAirlineFlightsData = new AirlineFlightsData(airlineFlightsData, true); // creating the copy of the backup

            IAirlineCommunication channel = getChannelByServerName(targetServer.airline);
            channel.moveAirline(copyAirlineFlightsData, this.currentVersion);

        }

        private ServerData findTargetServer(String airlineName, List<ServerData> allienceServers)
        {
            foreach (ServerData server in allienceServers)
            {
                bool serverNotContain = true;
                foreach (AirlineData a in server.airlines)
                {
                    if (a.name == airlineName)
                    {
                        serverNotContain = false;
                    }
                }
                if (serverNotContain)
                {
                    return server;
                }
            }
            
            // should never reach here
            Console.WriteLine("Code should never reach here");
            Console.WriteLine("airline server : " + this.getAirlineServers().First());
            return allienceServers[0];
        }

        private bool isBackedUp(string airline, List<ServerData> allienceServers)
        {
            int numberOfCopies = 0;
            
         //   if ( allienceServers.Count() == 1) return true;
            
            foreach (ServerData server in allienceServers)
            {
                foreach(AirlineData a in  server.airlines){
                    if (a.name == airline)
                    {
                        numberOfCopies++;
                    }
                }
            }

            return numberOfCopies >= AirlineCommunication.minimalNumberOfCopies;
        }

        public Boolean moveAirline(AirlineFlightsData airline, int phase)
        {

            
            List<AirlineFlightsData> airlineFlightDataList = new List<AirlineFlightsData>(this.getCurrentPhaseDate(phase).Values);
            foreach (AirlineFlightsData arFlightData in airlineFlightDataList)
            {
                // if exists, replace and return
                if (arFlightData.airlineName == airline.airlineName && arFlightData.backup == airline.backup)
                {
                    arFlightData.flights = airline.flights;
                    return true;
                }

            }
            // add new entry 
            this.getCurrentPhaseDate(phase).Add(airline.airlineName, airline);
            return true;
        }

        public List<ServerData> balance(List<ServerData> allienceServers, List<String> airlines,  int phase)
        {

            this.printData(allienceServers, "******** BALACE OLD DATA  - BEGIN ********");
            
            
            // if there is only 1 server no need for balance
            if (allienceServers.Count == 1)
            {
                serverData.Add(phase, getCurrentPhaseDate());
                return allienceServers;
            } 


            AirlinesFlightsData tmpAirlinesFlightsData = getCurrentPhaseDate();
            List<AirlineFlightsData> myAirline = new List<AirlineFlightsData>(tmpAirlinesFlightsData.Values);

            List<ServerData> nextPhaseImage = this.calcNextPhaseImage(allienceServers, airlines);

            foreach (AirlineFlightsData airline in myAirline){
                String tergetServer = this.calcTargetServerAfterBalance(airline, nextPhaseImage);

                IAirlineCommunication channel = getChannelByServerName(tergetServer);
                //send the data to the target server to the next phase
                channel.moveAirline(airline, phase);   

            }

            this.printData(allienceServers, "******** BALACE OLD DATA  - END ********");
            

            return nextPhaseImage;
        }

        private IAirlineCommunication getChannelByServerName(String serverName)
        {
            IAirlineCommunication channel;
            if (serverName == this.serverName)
            {
                channel = this;
            }
            else
            {
                this.serversChannels.TryGetValue(serverName, out channel);

            }
            return channel;
        }

        private string calcTargetServerAfterBalance(AirlineFlightsData airline, List<ServerData> nextPhaseImage)
        {
            foreach (ServerData s in nextPhaseImage)
            {
                 if (s.airlines.Exists(a=> (a.name == airline.airlineName && a.isPrimary != airline.backup))){
                      return s.airline;
                 }
            }

            Console.WriteLine("THIS CODE SHOULD NEVER BE CALLED!!!!!! THIS MEAN THAT THE AIRLINE DATE DOES NOT EXISTS IN THE NEW IMAGE");
            return airline.airlineName;
        }

        public List<ServerData> balance1(List<ServerData> allienceServers)
        { 
            List<String> airlines = new List<string>();
            
            List<ServerData> tmpServers = new List<ServerData>(allienceServers);

            foreach (ServerData s in tmpServers)
            {

                foreach (AirlineData air in s.airlines)
                {
                    if (!airlines.Contains(air.name))
                    {
                        airlines.Add(air.name);
                    }
                }
            }

            int phase = this.currentVersion +1;




            return this.balance(allienceServers, airlines, phase);
        }

        private List<ServerData> calcNextPhaseImage(List<ServerData> allienceServers, List<string> airlines)
        {
            List<ServerData> nextPhaseImage = new List<ServerData>(allienceServers);
            foreach(ServerData s in nextPhaseImage){
                s.airlines = new List<AirlineData>();
            }

            for (int i=0; i<airlines.Count(); i++ )
            {
                nextPhaseImage[i % nextPhaseImage.Count()].airlines.Add(new AirlineData(airlines[i], true));
            }

            // if There is only 1 server than you should not have backup
            if (nextPhaseImage.Count == 1) return nextPhaseImage;

            for (int i = 0; i < airlines.Count(); i++)
            {
                nextPhaseImage[(i+1) % nextPhaseImage.Count()].airlines.Add(new AirlineData(airlines[i], false));
            }

            return nextPhaseImage;
        }

      

        private string calcTargetServerAfterBalance(AirlineFlightsData airline, List<ServerData> allienceServers, List<string> airlines)
        {
            return "DefaultConnection";
        }

     
        public ConnectionFlights SearchAllServers(String src, String dst, DateTime date, String airline)
        {
            Flights srcDstFlights = new Flights();
            Flights srcFlights = new Flights();
            Flights dstFlightsDay1 = new Flights();
            Flights dstFlightsDay2 = new Flights();

            ConnectionFlights resFlights = new ConnectionFlights();


            foreach (ServerData server in this.distributer.getServers())
            {
                AirlinesFlights airlinesFlights = new AirlinesFlights();

                IAirlineCommunication channel = getChannelByServerName(server.airline);
                airlinesFlights = channel.Search(src, dst, date, airline);

                //if (this.serverName == server.airline)// local server
                //{
                //    airlinesFlights = this.Search(src, dst, date, airline);
                //}
                //else
                //{
                //    IAirlineCommunication channel;
                //    this.serversChannels.TryGetValue(server.airline, out channel);
                //    airlinesFlights = channel.Search(src, dst, date, airline);
                //}
                
                foreach (var airlineFlights in airlinesFlights.Values){
                    srcDstFlights.AddRange(airlineFlights.srcDstflights);
                    srcFlights.AddRange(airlineFlights.srcFlights);
                    dstFlightsDay1.AddRange(airlineFlights.dstDay1Flights);
                    dstFlightsDay2.AddRange(airlineFlights.dstDay2Flights);
                }
            }

            findFlightsCombination(srcDstFlights, srcFlights, dstFlightsDay1, dstFlightsDay2, resFlights);

            Console.WriteLine("Search all servers: " + "src: " + src + " " + "dst: " + dst + " " + "date: " + date + " ");

            return resFlights;
        }

        private static IAirlineCommunication createChannelForServer(ServerData server)
        {
            ServiceEndpoint httpEndpoint =
               new ServiceEndpoint(
               ContractDescription.GetContract(
               typeof(IAirlineCommunication)),
               new BasicHttpBinding(),
               new EndpointAddress
               (server.url + "/AirlineCommunication"));
            //// create channel factory based on HTTP endpoint
            ChannelFactory<IAirlineCommunication> channelFactory = new ChannelFactory<IAirlineCommunication>(httpEndpoint);
            IAirlineCommunication channel = channelFactory.CreateChannel();
            return channel;
        }


        public AirlinesFlights Search(String src, String dst, DateTime date, String airline)
        {
            AirlinesFlights resAirlinesFlights = new AirlinesFlights();

            //create connection to other servers - TODO should be outside the function

            ConnectionFlights resFlights = new ConnectionFlights();

            // algorithm 

            foreach (String ar in this.getAirlineServers())
            {
                Flights srcDstFlights = new Flights();
                Flights srcFlights = new Flights();
                Flights dstFlightsDay1 = new Flights();
                Flights dstFlightsDay2 = new Flights();
                Flights airlineFlight;

                if (airline == null || inSpecifiedAirlines(ar, airline)) // make the same request to all server
                {
                    airlineFlight = this.getAirlineFlightsWithSrc(ar,src, dst, date);
                     // get airline flight with all information
                }
                else
                {
                    airlineFlight = new Flights(); //this.getAirlineFlightsWithoutSrc(ar, src, dst, date); // get only conncetion filghts of this airline (destination)          
                }
                
                sortFlights(src, dst, date, srcDstFlights, srcFlights, dstFlightsDay1, dstFlightsDay2, airlineFlight);
                resAirlinesFlights.Add(ar, new AirlineFlights(srcDstFlights, srcFlights, dstFlightsDay1, dstFlightsDay2));
            }
           
            Console.WriteLine("Search one server: "+  this.serverName  + " src: " + src + " " + "dst: " + dst + " " + "date: " + date + " " + " airline: "+ airline);

            return resAirlinesFlights;
        }

        private IEnumerable<string> getAirlineServers()
        {
            AirlinesFlightsData serverData = getCurrentPhaseDate();
            return serverData.Keys;

        }

        private Flights getAirlineFlightsWithoutSrc(string ar, string src, string dst, DateTime date)
        {
            Flights resFlights = new Flights();
           
            AirlineFlightsData tmpAirlineFlightsData = getAirlineData(ar);
            foreach (Flight f in tmpAirlineFlightsData.flights)
            {
                if (f.Src != src && f.Dst == dst && f.Date == date)
                {
                    resFlights.Add(new Flight(f));
                }
                else if (f.Src != src && f.Dst == dst && f.Date == date.AddDays(1))
                {
                    resFlights.Add(new Flight(f));
                }

            }
            return resFlights;
        }

        private AirlineFlightsData getAirlineData(string ar)
        {
            AirlinesFlightsData tmpAllienceDataImage = new AirlinesFlightsData();
            serverData.TryGetValue(this.currentVersion, out tmpAllienceDataImage);

            AirlineFlightsData tmpAirlineFlightsData = new AirlineFlightsData();
            tmpAllienceDataImage.TryGetValue(ar, out tmpAirlineFlightsData);
            
            //if this is a backup return an empty list
            if (tmpAirlineFlightsData.backup)
            {
                tmpAirlineFlightsData = new AirlineFlightsData();
                return tmpAirlineFlightsData;
            }
            
            return tmpAirlineFlightsData;
        }

        private Flights getAirlineFlightsWithSrc(string ar, string src, string dst, DateTime date)
        {
            Flights resFlights = new Flights();
            AirlineFlightsData tmpAirlineFlightsData = getAirlineData(ar);
            foreach (Flight f in tmpAirlineFlightsData.flights)
            {
                if (f.Src == src && f.Dst == dst && f.Date == date)
                {
                    resFlights.Add(new Flight(f));
                }
                else if (f.Src == src && f.Dst != dst && f.Date == date)
                {
                    resFlights.Add(new Flight(f));
                }
                else if (f.Src != src && f.Dst == dst && f.Date == date)
                {
                    resFlights.Add(new Flight(f));
                }
                else if (f.Src != src && f.Dst == dst && f.Date == date.AddDays(1))
                {
                    resFlights.Add(new Flight(f));
                }

            }
            return resFlights;
        }


        /**
         *  check if the airline is in the specific airlines 
         */
        private bool inSpecifiedAirlines(string ar, string airlines)
        {
            if (airlines == null)
                return true;
            string[] airlinesArray = Array.ConvertAll(airlines.Trim().Split(' '), p => p.Trim());
            airlinesArray = airlinesArray.Where(item => item != "").ToArray();

            if (airlinesArray.Length == 0)
                return true;
            return airlinesArray.Contains(ar);
        }

        private static void findFlightsCombination(Flights srcDstFlights, Flights srcFlights, Flights dstFlightsDay1, Flights dstFlightsDay2, ConnectionFlights resFlights)
        {
            foreach (Flight f in srcDstFlights)
            {
                resFlights.Add(new ConnectionFlight(f));
            }

            foreach (Flight f1 in srcFlights)
            {
                foreach (Flight f2 in dstFlightsDay1)
                {
                    if (f1.Dst == f2.Src)
                    {
                        resFlights.Add(new ConnectionFlight(f1, f2));
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
        }

        private static void sortFlights(String src, String dst, DateTime date, Flights srcDstFlights, Flights srcFlights, Flights dstFlightsDay1, Flights dstFlightsDay2, Flights airlineFlight)
        {
            foreach (Flight f in airlineFlight)
            {
                if (f.Src == src && f.Dst == dst)
                {
                    srcDstFlights.Add(f);
                }
                else if (f.Src == src)
                {
                    srcFlights.Add(f);
                }
                else if (f.Dst == dst && f.Date == date)
                {
                    dstFlightsDay1.Add(f);
                }
                else if (f.Dst == dst && f.Date == date.AddDays(1))
                {
                    dstFlightsDay2.Add(f);
                }
            }
        }

        // read flights from file
        private void initializeFlights(String fileName)
        {
            String line;
            System.IO.StreamReader file =
                 new System.IO.StreamReader(fileName);
            Flight f;
            List<Flight> flights = new List<Flight>();

            while ((line = file.ReadLine()) != null)
            {
                String[] words = line.Split();
                f = new Flight();
                f.Seller = this.serverName;
                f.FNum = words[0];
                f.Src = words[1];
                f.Dst = words[2];
                f.Date = DateTime.ParseExact(words[3], "dd/MM/yyyy", CultureInfo.InvariantCulture);
                f.AvailableSeats = 10;// Convert.ToInt32(words[4]);
                f.Price = Convert.ToInt32(words[4]);
                flights.Add(f);
            }

           
            // initialize airlineFlightData
            AirlineFlightsData airlineFlightsData = new AirlineFlightsData();
            airlineFlightsData.airlineName = this.serverName;
            airlineFlightsData.flights =  new Flights(flights);
            airlineFlightsData.backup = false;
            
            AirlinesFlightsData allienceDataImage = new AirlinesFlightsData();
            allienceDataImage.Add(this.serverName, airlineFlightsData);
            serverData.Add(this.currentVersion, allienceDataImage);

            file.Close();
        }

        private int getCurrentPhase()
        {
            return 0;
        }
    }
}
