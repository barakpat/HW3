using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
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
            //ServiceEndpoint ep = clienthost.AddServiceEndpoint(typeof(ISearchService), new WebHttpBinding(), "");

            LogBehavior logBehavior = new LogBehavior(args[2]);
            ServiceEndpoint endPoint = clienthost.AddServiceEndpoint(typeof(ISearchService), new WebHttpBinding(), "");
            foreach (OperationDescription od in endPoint.Contract.Operations)
            {
                od.Behaviors.Add(logBehavior);
            }


            clienthost.Open();

            WebServiceHost sellerhost = new WebServiceHost(typeof(SellerService), new Uri("http://localhost:" + args[1] + "/Services/FlightsSearchReg"));
            sellerhost.Open();

            Console.ReadKey();

            clienthost.Close();
            sellerhost.Close();
        }
    }
}
