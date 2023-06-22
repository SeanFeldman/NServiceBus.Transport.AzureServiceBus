namespace Trigger;

using System;
using System.Threading.Tasks;
using Contracts;
using NServiceBus;



public class ProcessingInstructionHandler : IHandleMessages<ProcessInstruction>
{
    public async Task Handle(ProcessInstruction message, IMessageHandlerContext context)
    {
        Console.WriteLine($"Processing ProcessInstruction message, ID {message.Id}");
#pragma warning disable NSB0002
        await Task.Delay(200).ConfigureAwait(false);
#pragma warning restore NSB0002
        Console.WriteLine($"Done processInstruction message, ID {message.Id}");
    }
}