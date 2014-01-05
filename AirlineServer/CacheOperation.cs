using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

namespace AirlineServer
{
    class CacheOperation :IDispatchMessageInspector
    {

        private Dictionary<string, int> mapActionToCount = new Dictionary<string, int>(); 
        
        public object AfterReceiveRequest( ref System.ServiceModel.Channels.Message request, IClientChannel channel, InstanceContext instanceContext) 
        { 
            if (!mapActionToCount.ContainsKey(request.Headers.Action)) 
            { 
                mapActionToCount.Add(request.Headers.Action.ToString(), 0); 
            } 
            mapActionToCount[request.Headers.Action]++; return null; 
        } 
        public void BeforeSendReply( ref System.ServiceModel.Channels.Message reply, object correlationState) 
        { 
            // nothing to do here 
        } 
    }
}
