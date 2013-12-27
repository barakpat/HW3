using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HW1c
{
    
    [DataContract] public class Flight {
        public Flight() {}
        public Flight(Flight f)
        {
            this.Seller = f.Seller;
            this.Src = f.Src;
            this.Dst = f.Dst;
            this.FNum = f.FNum;
            this.AvailableSeats = f.AvailableSeats;
            this.Price = f.Price;
            this.Date = f.Date;
        }
        [DataMember]
        public String Src { get; set; }
        [DataMember]
        public String Dst { get; set; }
        [DataMember]
        public String Seller { get; set; }
        [DataMember]
        public String FNum { get; set; }
        [DataMember]
        public int AvailableSeats { get; set; }
        [DataMember]
        public int Price { get; set; }
        [DataMember]
        public DateTime Date { get; set; } 

    }

    [DataContract]
    public class Reservation
    {
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public String Seller { get; set; }
        [DataMember]
        public String FNum { get; set; }
        [DataMember]
        public DateTime Date { get; set; }
    }

    [DataContract]
    public class ConnectionFlight
    {
        public ConnectionFlight() { }
        public ConnectionFlight(Flight f)
        {
            this.flight1 = f;
            this.flight2 = null;
            this.price = f.Price;
        }
        public ConnectionFlight(Flight f1, Flight f2)
        {
            this.flight1 = f1;
            this.flight2 = f2;
            this.price = f1.Price + f2.Price;
        }
        [DataMember]
        public Flight flight1 { get; set; }
        [DataMember]
        public Flight flight2 { get; set; }
        [DataMember]
        public int price { get; set; }
    }

    [CollectionDataContract]
    public class ConnectionFlights : List<ConnectionFlight>
    {
        public ConnectionFlights() { }
        public ConnectionFlights(List<ConnectionFlight> flights) : base(flights) { }
    }


    [CollectionDataContract]
    public class Flights : List<Flight>
    {
        public Flights() { }
        public Flights(List<Flight> flights) : base(flights) { }
    }
    
    [CollectionDataContract]
    public class Airlines : List<String>
    {
        public Airlines() { }
        public Airlines(List<String> airlines) : base(airlines) { }
    }

    [DataContract]
    public class AllianceDelegate
    {
        [DataMember]
        public Boolean isDelegate { get; set; }
        [DataMember]
        public String AirlineName { get; set; }
        [DataMember]
        public String AllianceName { get; set; }
        [DataMember]
        public String Port { get; set; }
        [DataMember]
        public Uri ServiceURI { get; set; }
    }

}