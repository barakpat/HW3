using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Net;
using HW1c;


// HW3 client 
namespace Client
{
    class Program
    {
        static String dateFormat = "dd/MM/yyyy";

        static void Main(string[] args)
        {
            WebChannelFactory<ISearchService> cf = new WebChannelFactory<ISearchService>(new Uri(args[0]));
            ISearchService channel = cf.CreateChannel();
        
            String input  = Console.ReadLine();

            while (input != "exit"){
                try
                {
                    string[] words = input.Split(' ');
                
                    if (words[0] == "search")
                    {
                        String src;
                        String dst;
                        DateTime date;
                        try
                        {
                            src = words[1];
                            dst = words[2];
                            date = DateTime.ParseExact(words[3], dateFormat, CultureInfo.InvariantCulture);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Failed, Invalid Input");
                            continue;
                        }
                        Flights flights =null;
                        try
                        {
                            flights = channel.GetFlights(src, dst, date.ToString(dateFormat));
                        }
                        catch(Exception e){
                            continue;
                        }

                        
                        flights.Sort(delegate(Flight f1, Flight f2)
                        {
                            if (f1.Price > f2.Price) return 1;
                            else if (f1.Price < f2.Price) return -1;
                            else
                            {
                                if (f1.AvailableSeats < f2.AvailableSeats) return 1;
                                else if (f1.AvailableSeats > f2.AvailableSeats) return -1;
                                else return f1.FNum.CompareTo(f2.FNum);
                            }

                        });

                        flights = new Flights(flights.Where(f => f.AvailableSeats > 0).ToList());


                        if (!flights.Any() )
                        {
                            Console.WriteLine("No flights available");
                        }
                        else
                        {
                            foreach (Flight f in flights)
                            {
                                Console.WriteLine("{0} {1} {2} seats {3}$", f.Seller, f.FNum, f.AvailableSeats, f.Price);
                            }
                        }


                    }
                    else if (words[0] == "reserve")
                    {
                        Reservation r = new Reservation();
                        try
                        {
                            r.Seller = words[1];
                            r.FNum = words[2];
                            r.Date = DateTime.ParseExact(words[3], dateFormat, CultureInfo.InvariantCulture);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Failed, Invalid Input");
                            continue;
                        }
                        
                        try
                        {
                            int id = channel.makeReservation(r);
                            Console.WriteLine("OK, reservation ID: " + id);
                        }
                        catch (Exception e)
                        {
                            if (e.InnerException is WebException)
                            {
                                HttpWebResponse resp = (HttpWebResponse)((WebException)e.InnerException).Response;
                                Console.WriteLine("Failed, " + resp.StatusDescription);
                            }
                            else
                            {
                                Console.WriteLine("Failed, " + e.Message);
                            }
                        }
                    }
                    else if (words[0] == "cancel")
                    {
                        Reservation r = new Reservation();
                        try
                        {
                            r.Seller = words[1];
                            r.Id = Convert.ToInt32(words[2]);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Failed, Invalid Input");
                            continue;
                        }

                        try
                        {
                            channel.makeCancelation(r.Seller, Convert.ToString(r.Id));
                            Console.WriteLine("OK");
                        }
                        catch (Exception e)
                        {
                            if (e.InnerException is WebException)
                            {
                                HttpWebResponse resp = (HttpWebResponse)((WebException)e.InnerException).Response;
                                Console.WriteLine("Failed, " + resp.StatusDescription);
                            }
                            else
                            {
                                Console.WriteLine("Failed, " + e.Message);
                            }
                        }                        
                    }
                    else
                    {
                        Console.WriteLine("Failed, Invalid Input");
                    }
                }
                catch (Exception e) 
                {
                }
                finally {
                    // read  new line
                    input = Console.ReadLine();
                }
            }
            
        }
    }
}
