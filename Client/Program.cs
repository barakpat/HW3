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
                        String airlines = "";
                        try
                        {
                            src = words[1];
                            dst = words[2];
                            date = DateTime.ParseExact(words[3], dateFormat, CultureInfo.InvariantCulture);
                            for (int i=4 ; i<words.Length ; i++){
                                airlines += words[i] + " ";
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Failed, Invalid Input");
                            continue;
                        }
                        ConnectionFlights flights = null;
                        

                        //airlines.Add("KLM");
                        
                        try
                        {
                            flights = channel.GetFlights(src, dst, date.ToString(dateFormat), airlines);
                        }
                        catch(Exception e){
                            continue;
                        }


                        flights.Sort(delegate(ConnectionFlight f1, ConnectionFlight f2)
                        {
                            return f1.price.CompareTo(f2.price);
                        });

                        //flights = new Flights(flights.Where(f => f.AvailableSeats > 0).ToList());


                        if (!flights.Any() )
                        {
                            Console.WriteLine("No flights available");
                        }
                        else
                        {
                            foreach (ConnectionFlight f in flights)
                            {
                                //400$: tlv-par (air-france, af1234), par-jfk (air-france, af321)
                                if (f.flight2 == null)
                                {
                                    Console.WriteLine("{0}$: {1}-{2} ({3}, {4})", f.price, f.flight1.Src, f.flight1.Dst, f.flight1.Seller, f.flight1.FNum);
                                }
                                else
                                {
                                     Console.WriteLine("{0}$: {1}-{2} ({3}, {4}), {5}-{6} ({7}, {8})", f.price, f.flight1.Src, f.flight1.Dst, f.flight1.Seller, f.flight1.FNum,
                                                                                                         f.flight2.Src, f.flight2.Dst, f.flight2.Seller, f.flight2.FNum);
                                }
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
