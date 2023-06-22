#pragma warning disable IDE0065
using NServiceBus;
#pragma warning restore IDE0065

namespace Contracts;

public class ProcessInstruction : ICommand
{
    public int Id { get; set; }
}