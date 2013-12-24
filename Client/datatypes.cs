using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HW1c
{
    
    //Barak just commited something on barak branch

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

    [CollectionDataContract]
    public class Flights : List<Flight>
    {
        public Flights() { }
        public Flights(List<Flight> flights) : base(flights) { }
    }

    [DataContract]
    public class TicketServer
    {
        [DataMember]
        public String ServerName { get; set; }
        [DataMember]
        public String Port { get; set; }
        [DataMember]
        public Uri ServiceURI { get; set; }
    }

}