using HW1c;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace TicketSellingServer
{
    [ServiceContract]
    interface ITicketSellingServerSoap
    {
        [OperationContract]
        Flights Search(String src, String dst, DateTime date);

        [OperationContract]
        int Reserve(Reservation reservation);

        [OperationContract]
        void Cancel(int id);
        
    }

}
