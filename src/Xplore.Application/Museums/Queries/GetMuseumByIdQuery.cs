namespace Xplore.Application.Museums.Queries;

using MediatR;
using Xplore.Domain.Entities;

/// <summary>
/// Query to get a museum by ID.
/// </summary>
public record GetMuseumByIdQuery(Guid Id) : IRequest<Museum?>;
