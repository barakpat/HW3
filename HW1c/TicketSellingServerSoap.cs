﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.5466
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

  /*
    
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
[System.ServiceModel.ServiceContractAttribute(ConfigurationName="ITicketSellingServerSoap")]
public interface ITicketSellingServerSoap
{
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ITicketSellingServerSoap/Search", ReplyAction="http://tempuri.org/ITicketSellingServerSoap/SearchResponse")]
    HW1c.Flights Search(string src, string dst, System.DateTime date);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ITicketSellingServerSoap/Reserve", ReplyAction="http://tempuri.org/ITicketSellingServerSoap/ReserveResponse")]
    int Reserve(HW1c.Reservation reservation);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ITicketSellingServerSoap/Cancel", ReplyAction="http://tempuri.org/ITicketSellingServerSoap/CancelResponse")]
    void Cancel(int id);
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
public interface ITicketSellingServerSoapChannel : ITicketSellingServerSoap, System.ServiceModel.IClientChannel
{
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
public partial class TicketSellingServerSoapClient : System.ServiceModel.ClientBase<ITicketSellingServerSoap>, ITicketSellingServerSoap
{
    
    public TicketSellingServerSoapClient()
    {
    }
    
    public TicketSellingServerSoapClient(string endpointConfigurationName) : 
            base(endpointConfigurationName)
    {
    }
    
    public TicketSellingServerSoapClient(string endpointConfigurationName, string remoteAddress) : 
            base(endpointConfigurationName, remoteAddress)
    {
    }
    
    public TicketSellingServerSoapClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
            base(endpointConfigurationName, remoteAddress)
    {
    }
    
    public TicketSellingServerSoapClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
            base(binding, remoteAddress)
    {
    }
    
    public HW1c.Flights Search(string src, string dst, System.DateTime date)
    {
        return base.Channel.Search(src, dst, date);
    }
    
    public int Reserve(HW1c.Reservation reservation)
    {
        return base.Channel.Reserve(reservation);
    }
    
    public void Cancel(int id)
    {
        base.Channel.Cancel(id);
    }
}
   */
