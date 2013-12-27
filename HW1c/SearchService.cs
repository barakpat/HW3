using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace HW1c
{

    public class SearchService : ISearchService
    {

        public ConnectionFlights GetFlights(String src, String dst, String date, String airlines)
        {
            ConnectionFlights res = new ConnectionFlights();
            Console.WriteLine("getFlights src=" + src);
            DateTime newDate;
            if (!this.getFlightCheckParams(src, dst, date, out newDate))
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = "src & dst must not be empty and date must be valid DateTime i.e 15/11/2012";
                return res;
            }

            string[] airlinesArray = Array.ConvertAll(airlines.Trim().Split(' '), p => p.Trim());
            airlinesArray = airlinesArray.Where(item => item != "").ToArray();
            //string[] airlinesArray = airlines.Split(' ');

            if (airlinesArray.Length > 0) // specific airlines were specified
            {
                Console.Write("the airline are: ");
                foreach (String airline in airlinesArray){
                    String server=null;
                    IAirlineServerSoap ts;
                    TicketServerList.airlineToServer.TryGetValue(airline,out server);
                    if (server==null) continue;

                    TicketServerList.ticketServersProxies.TryGetValue(server, out ts);
                    try
                    {
                        using (
                        new OperationContextScope((IContextChannel)ts))
                        {
                            ConnectionFlights flightsRes = ts.Search(src, dst, newDate);
                            res.AddRange(flightsRes);
                        }
                    }
                    catch (FaultException e)
                    {
                        Console.WriteLine("service failed: {0}", e.Reason);
                    }
                    catch (Exception e)
                    {

                    }
                }
            }
            else{
                foreach (var ts in TicketServerList.ticketServersProxies.Values)
                {
                    try
                    {
                        using (
                        new OperationContextScope((IContextChannel)ts))
                        {
                            ConnectionFlights flightsRes = ts.Search(src, dst, newDate);
                            res.AddRange(flightsRes);
                        }
                    }
                    catch (FaultException e)
                    {
                        Console.WriteLine("service failed: {0}", e.Reason);
                    }
                    catch (Exception e)
                    {

                    }
                }
            }
            

            return res; 
            
        }

        private bool getFlightCheckParams(string src, string dst, string date, out DateTime dateout)
        {
            dateout = new DateTime();
            try
            {
                if (!Convert.ToBoolean(src.Length) || !Convert.ToBoolean(dst.Length))
                {
                    return false;
                }
                dateout = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                return true;
            }
            catch(Exception e){
                return false;
            }
        }

        public int makeReservation(Reservation reservation)
        {

            if (reservation == null || reservation.Seller == null || reservation.Seller.Equals("") || reservation.FNum == null || reservation.FNum.Equals("")  || reservation.Date == null)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = "reservetion must be a Reservetion Object with seller(i.e 'elal') FNum(i.e dk23) and Date (i.e 10/11/2013)";
                return -1;
            }
            
            Console.WriteLine("reservation = " + reservation.Seller + " " + reservation.FNum + " " + reservation.Date);

            IAirlineServerSoap ts= null;
            if (!TicketServerList.ticketServersProxies.TryGetValue(reservation.Seller, out ts))
            {
                WebOperationContext.Current.OutgoingResponse.SetStatusAsNotFound("unknown seller");
                return -1;
            }
            int id = 0;

            try
            {
                using (
                    new OperationContextScope((IContextChannel)ts))
                {
                   // id = ts.Reserve(reservation);
                }
            }
            catch (FaultException e)
            {
                WebOperationContext.Current.OutgoingResponse.SetStatusAsNotFound(e.Reason.ToString());
                return -1;
            }
            catch (Exception e) 
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = reservation.Seller + " server is down";
                return -1;
            }
            // a new reservation has created
            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Created;
            return id;
            
        }

        public void makeCancelation(String seller, String id)
        {
            Console.WriteLine("cancelation = " + id + " " + seller);
            int myId = 0;
            if (!this.makeCancelationCheckParams(seller, id, out myId))
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = "seller must not be empty and id must be valid nuber";
                return;
            }
            
            IAirlineServerSoap ts = null;
            if (!TicketServerList.ticketServersProxies.TryGetValue(seller, out ts))
            {
                WebOperationContext.Current.OutgoingResponse.SetStatusAsNotFound("unknown seller");
                return ;
            }
            try
            {
                using (
                    new OperationContextScope((IContextChannel)ts))
                {
                    //ts.Cancel(myId);
                }
            }
            catch (FaultException e)
            {
                WebOperationContext.Current.OutgoingResponse.SetStatusAsNotFound(e.Reason.ToString());
                return;
            }
            catch (Exception e) 
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = seller + " server is down";
                return;
            }
            // set code to 204 because delete was successful 
            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NoContent;
            
        }


        private bool makeCancelationCheckParams(string seller,string id, out int myId )
        {
            myId = 0;
            try
            {
                if (!Convert.ToBoolean(seller.Length))
                {
                    return false;
                }
                myId = Convert.ToInt32(id);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        
    }

}
