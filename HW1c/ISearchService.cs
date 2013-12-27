using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace HW1c
{
    [ServiceContract]
    public interface ISearchService
    {
        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "flights?src={src}&dst={dst}&date={date}&airlines={airlines}")]
        ConnectionFlights GetFlights(String src, String dst, String date, String airlines);
//ooppss
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "reservations")]
        int makeReservation(Reservation reservation);

        [OperationContract]
        [WebInvoke(Method = "DELETE", UriTemplate = "reservations/{seller}/{id}")]
        void makeCancelation(String seller, String id);      

    }
}
