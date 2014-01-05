using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace HW1c
{
    class LogBehavior : IOperationBehavior
    {

        public String file;

        public LogBehavior(String file)
        {
            this.file = file;
        }

        public void AddBindingParameters(OperationDescription operationDescription, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
            return;
        }

        public void ApplyClientBehavior(OperationDescription operationDescription, System.ServiceModel.Dispatcher.ClientOperation clientOperation)
        {
            return;
        }

        public void ApplyDispatchBehavior(OperationDescription operationDescription, System.ServiceModel.Dispatcher.DispatchOperation dispatchOperation)
        {
            dispatchOperation.Invoker = new LogInvoker(this.file, dispatchOperation.Invoker);
        }

        public void Validate(OperationDescription operationDescription)
        {
            return;
        }
    }
}


