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
    public interface ISellerService
    {
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "ticketSetvers",
             BodyStyle = WebMessageBodyStyle.Wrapped)]
        bool registerServer(TicketServer ticketServer);

        [OperationContract]
        [WebInvoke(Method = "DELETE", UriTemplate = "ticketSetvers/{seller}",
             BodyStyle = WebMessageBodyStyle.Wrapped)]
        bool unregisterServer(String seller);

    }
}

