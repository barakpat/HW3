using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace HW1c
{
    class SellerService : ISellerService
    {
        public bool registerServer(TicketServer ticketServer)
        {

            if (ticketServer == null || ticketServer.ServiceURI == null || ticketServer.ServiceURI.Equals("") || ticketServer.ServerName == null || ticketServer.ServerName.Equals(""))
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = "ticketServer must be a TicketServer Object with ServerName not empty (i.e 'elal') and ServiceURI is a URI (i.e http://localhost:8200/services/TicketSellingServerSoap)";
                return false;
            }
            ServiceEndpoint httpEndpoint =
               new ServiceEndpoint(
               ContractDescription.GetContract(
               typeof(ITicketSellingServerSoap)),
               new BasicHttpBinding(),
               new EndpointAddress
               (ticketServer.ServiceURI));
            //// create channel factory based on HTTP endpoint
            ChannelFactory<ITicketSellingServerSoap> channelFactory = new ChannelFactory<ITicketSellingServerSoap>(httpEndpoint);
            ITicketSellingServerSoap channel = channelFactory.CreateChannel();
            // adding the seller name and the channel factory
            TicketServerList.ticketServersProxies.AddOrUpdate(ticketServer.ServerName, channel, (key, oldVal) => channel);

            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Created;
            return true;
        }

        public bool unregisterServer(String serverName)
        {
            if (serverName == null || serverName.Equals("") )
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = "serverName must not be empty (i.e 'elal')";
                return false;
            }
            
            ITicketSellingServerSoap channel;
            if (!TicketServerList.ticketServersProxies.TryRemove(serverName, out channel))
            {
                WebOperationContext.Current.OutgoingResponse.SetStatusAsNotFound("unknown seller");
                return false;
            }

            // set code to 204 because delete was successful 
            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NoContent;
            return true;
        }

    }
}
