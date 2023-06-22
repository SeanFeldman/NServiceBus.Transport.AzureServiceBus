using System;
using NServiceBus;
using Secrets;

Console.WriteLine("Processor");

var configuration = new EndpointConfiguration("processor");
var transport = configuration.UseTransport<AzureServiceBusTransport>();
transport.ConnectionString(Connections.AzureServiceBus);

configuration.SendFailedMessagesTo("error");
configuration.Recoverability().Delayed(settings => settings.NumberOfRetries(0));
configuration.Recoverability().Immediate(settings => settings.NumberOfRetries(0));
configuration.EnableInstallers();

_ = await Endpoint.Start(configuration).ConfigureAwait(false);
Console.ReadLine();
