using HW1c;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

namespace AirlineServer
{
    class CacheInvoker : IOperationInvoker
    {
        private IOperationInvoker invoker;
        Cache cache = null;
        String fileName = null;

        public CacheInvoker(Cache inputCache, IOperationInvoker invoker)
        {
            this.invoker = invoker;
            cache = inputCache;
        }

        public object[] AllocateInputs()
        {
            return invoker.AllocateInputs();
        }


        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {

            outputs = new object[1];
            outputs[0] = new object();

            string key = inputs[0] + " " + inputs[1] + " " + (DateTime)inputs[2] + " " + inputs[3];
            Object result = cache.getFromCache(key);

            if (result == null)
            {
                result = invoker.Invoke(instance, inputs, out outputs);
                cache.insertToCache(key,(ConnectionFlights)result);
            }

            return result;
        }

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            return invoker.InvokeBegin(instance, inputs, callback, state);
        }

        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            return invoker.InvokeEnd(instance, out outputs, result);
        }

        public bool IsSynchronous
        {
            get { return invoker.IsSynchronous; }
        }
    }
}
