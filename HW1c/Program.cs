using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace HW1c
{
    class Program
    {
        static void Main(string[] args)
        {

            WebServiceHost clienthost = new WebServiceHost(typeof(SearchService), new Uri("http://localhost:" + args[0] + "/Services/FlightsSearch"));
            clienthost.Open();

            WebServiceHost sellerhost = new WebServiceHost(typeof(SellerService), new Uri("http://localhost:" + args[1] + "/Services/FlightsSearchReg"));
            sellerhost.Open();

            Console.ReadKey();
        }
    }
}
