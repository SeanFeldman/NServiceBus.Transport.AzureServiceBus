namespace NServiceBus.Transport.AzureServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Azure.Messaging.ServiceBus;
    using Azure.Messaging.ServiceBus.Administration;
    using Features;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    public class When_operating_with_least_privilege : NServiceBusAcceptanceTest
    {
        const string DedicatedTopic = "bundle-no-manage-rights";

        [SetUp]
        public async Task Setup()
        {
            var adminClient =
                new ServiceBusAdministrationClient(
                    Environment.GetEnvironmentVariable("AzureServiceBus_ConnectionString"));
            try
            {
                // makes sure during local development the topic gets cleared before each test run
                await adminClient.DeleteTopicAsync(DedicatedTopic);
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
            {
            }
        }

        [Test]
        public async Task Should_allow_message_operations_on_existing_resources()
        {
            Requires.NativePubSubSupport();

            // Run the scenario first with manage rights to make sure the topology is created
            await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.When(session => session.SendLocal(new MyCommand())))
                .WithEndpoint<Subscriber>()
                .Done(c => c.SubscriberGotEvent)
                .Run();

            // Now we run without manage rights
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b =>
                {
                    b.CustomConfig(c =>
                    {
                        // least-privilege mode doesn't support installers
                        c.GetSettings().Set("Installers.Enable", false);
                        // least-privilege mode doesn't support auto subscribe
                        c.DisableFeature<AutoSubscribe>();

                        var transport = c.ConfigureTransport<AzureServiceBusTransport>();
                        transport.ConnectionString =
                            Environment.GetEnvironmentVariable("AzureServiceBus_ConnectionString_Restricted");
                    });
                    b.When(session => session.SendLocal(new MyCommand()));
                })
                .WithEndpoint<Subscriber>(b =>
                {
                    b.CustomConfig(c =>
                    {
                        // least-privilege mode doesn't support installers
                        c.GetSettings().Set("Installers.Enable", false);
                        // least-privilege mode doesn't support auto subscribe
                        c.DisableFeature<AutoSubscribe>();

                        var transport = c.ConfigureTransport<AzureServiceBusTransport>();
                        transport.ConnectionString =
                            Environment.GetEnvironmentVariable("AzureServiceBus_ConnectionString_Restricted");
                    });
                })
                .Done(c => c.SubscriberGotEvent)
                .Run();

            Assert.True(context.SubscriberGotEvent);
        }

        public class Context : ScenarioContext
        {
            public bool SubscriberGotEvent { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher() =>
                EndpointSetup<DefaultPublisher>(b =>
                {
                    var transport = b.ConfigureTransport<AzureServiceBusTransport>();
                    transport.TopicName = DedicatedTopic;
                });

            public class MyHandler : IHandleMessages<MyCommand>
            {
                public Task Handle(MyCommand message, IMessageHandlerContext context)
                    => context.Publish(new MyEvent());
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
                => EndpointSetup<DefaultServer>(b
                    =>
                {
                    var transport = b.ConfigureTransport<AzureServiceBusTransport>();
                    transport.TopicName = DedicatedTopic;
                });

            public class MyHandler : IHandleMessages<MyEvent>
            {
                public MyHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    testContext.SubscriberGotEvent = true;
                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }
        }

        public class MyEvent : IEvent
        {
        }

        public class MyCommand : ICommand
        {
        }
    }
}