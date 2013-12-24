using HW1c;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace AirlineServer
{
    
    [ServiceContract]
    interface IAirlineServerSoap
    {
        [OperationContract]
        Flights Search(String src, String dst, DateTime date);

    }
}
