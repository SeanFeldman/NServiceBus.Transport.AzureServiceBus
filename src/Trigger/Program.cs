using System;
using Contracts;
using NServiceBus;
using Secrets;

Console.WriteLine("Trigger");

var configuration = new EndpointConfiguration("trigger");
var transport = configuration.UseTransport<AzureServiceBusTransport>();
transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
transport.ConnectionString(Connections.AzureServiceBus);

configuration.SendFailedMessagesTo("error");
configuration.Recoverability().Delayed(settings => settings.NumberOfRetries(0));
configuration.Recoverability().Immediate(settings => settings.NumberOfRetries(0));
configuration.EnableInstallers();

var endpoint = await Endpoint.Start(configuration).ConfigureAwait(false);

while (Console.ReadLine().Equals("s"))
{
    await endpoint.SendLocal(new Kickoff()).ConfigureAwait(false);
    Console.WriteLine("Kickoff message sent");
}