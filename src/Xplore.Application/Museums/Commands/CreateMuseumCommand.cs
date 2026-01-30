namespace Xplore.Application.Museums.Commands;

using MediatR;

/// <summary>
/// Command to create a new museum.
/// </summary>
public record CreateMuseumCommand(string Name, string Description) : IRequest<Guid>;
