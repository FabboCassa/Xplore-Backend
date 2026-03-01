using MassTransit;
using Microsoft.Extensions.Logging;
using Xplore.Contracts;

namespace Xplore.Worker.Consumers;

public class MuseumCreatedConsumer : IConsumer<MuseumCreatedEvent>
{
    private readonly ILogger<MuseumCreatedConsumer> _logger;

    public MuseumCreatedConsumer(ILogger<MuseumCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<MuseumCreatedEvent> context)
    {
        _logger.LogInformation("Message received! Museum created: {Name} (ID: {Id})",
            context.Message.Name, context.Message.Id);

        return Task.CompletedTask;
    }
}