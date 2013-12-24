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
        public bool registerServer(AllianceDelegate allienceDelegate)
        {

            if (allienceDelegate == null || allienceDelegate.ServiceURI == null || allienceDelegate.ServiceURI.Equals("") || allienceDelegate.AllianceName == null || allienceDelegate.AllianceName.Equals(""))
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = "Allience must be an Allience Object with AllienceName not empty (i.e 'elal') and ServiceURI is a URI (i.e http://localhost:8200/services/TicketSellingServerSoap)";
                return false;
            }

            TicketServerList.airlineToServer.AddOrUpdate(allienceDelegate.AirlineName, allienceDelegate.AllianceName, (key, oldVal) => allienceDelegate.AllianceName);
            
            // if airline is  delegate  
            if (allienceDelegate.isDelegate)
            {
                ServiceEndpoint httpEndpoint =
                   new ServiceEndpoint(
                   ContractDescription.GetContract(
                   typeof(IAirlineServerSoap)),
                   new BasicHttpBinding(),
                   new EndpointAddress
                   (allienceDelegate.ServiceURI));
                //// create channel factory based on HTTP endpoint
                ChannelFactory<IAirlineServerSoap> channelFactory = new ChannelFactory<IAirlineServerSoap>(httpEndpoint);
                IAirlineServerSoap channel = channelFactory.CreateChannel();
                // adding the seller name and the channel factory
                TicketServerList.ticketServersProxies.AddOrUpdate(allienceDelegate.AllianceName, channel, (key, oldVal) => channel);
            }

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
            
            IAirlineServerSoap channel;
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
