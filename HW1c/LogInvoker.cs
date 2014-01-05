using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel.Dispatcher;
using System.Text;

namespace HW1c
{
    class LogInvoker : IOperationInvoker
    {
        private IOperationInvoker invoker;
        String file = null;

        public LogInvoker(String file, IOperationInvoker invoker)
        {
            this.invoker = invoker;
            this.file = file;
        }

        public object[] AllocateInputs()
        {
            return invoker.AllocateInputs();
        }


        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            outputs = new object[1];
            outputs[0] = new object();

            string input = "";
            input = "Query : src: " + inputs[0] + " dst: " + inputs[1] + " date: " + inputs[2] + " airlines: " + inputs[3] + "\n";

            Object result = invoker.Invoke(instance, inputs, out outputs);
            sw.Stop();

            double duration = sw.Elapsed.TotalMilliseconds;

            string logEntry = input + "Execution time: " + duration.ToString() + " milisec";

            using (StreamWriter writer = new StreamWriter(this.file, true))
            {
                writer.WriteLine(logEntry);
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
