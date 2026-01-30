namespace Xplore.Application.Museums.Commands;

using MassTransit;
using MediatR;
using Xplore.Contracts;
using Xplore.Domain.Entities;

/// <summary>
/// Handler for CreateMuseumCommand. Creates a museum and publishes an event.
/// </summary>
public class CreateMuseumCommandHandler : IRequestHandler<CreateMuseumCommand, Guid>
{
    private readonly IPublishEndpoint _publishEndpoint;
    // TODO: Inject IMuseumRepository when implemented

    public CreateMuseumCommandHandler(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Guid> Handle(CreateMuseumCommand request, CancellationToken cancellationToken)
    {
        // Create the museum entity
        var museum = new Museum
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        // TODO: Save to database via repository
        // await _museumRepository.AddAsync(museum, cancellationToken);

        // Publish domain event
        await _publishEndpoint.Publish(
            new MuseumCreatedEvent(museum.Id, museum.Name), 
            cancellationToken);

        return museum.Id;
    }
}
