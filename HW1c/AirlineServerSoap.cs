﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.5466
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------



[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
[System.ServiceModel.ServiceContractAttribute(ConfigurationName = "IAirlineServerSoap")]
public interface IAirlineServerSoap
{

    [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IAirlineServerSoap/Search", ReplyAction = "http://tempuri.org/IAirlineServerSoap/SearchResponse")]
    HW1c.Flights Search(string src, string dst, System.DateTime date);

}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
public interface IAirlineServerSoapChannel : IAirlineServerSoap, System.ServiceModel.IClientChannel
{
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
public partial class AirlineServerSoapClient : System.ServiceModel.ClientBase<IAirlineServerSoap>, IAirlineServerSoap
{

    public AirlineServerSoapClient()
    {
    }

    public AirlineServerSoapClient(string endpointConfigurationName) :
        base(endpointConfigurationName)
    {
    }

    public AirlineServerSoapClient(string endpointConfigurationName, string remoteAddress) :
        base(endpointConfigurationName, remoteAddress)
    {
    }

    public AirlineServerSoapClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) :
        base(endpointConfigurationName, remoteAddress)
    {
    }

    public AirlineServerSoapClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) :
        base(binding, remoteAddress)
    {
    }

    public HW1c.Flights Search(string src, string dst, System.DateTime date)
    {
        return base.Channel.Search(src, dst, date);
    }

}
