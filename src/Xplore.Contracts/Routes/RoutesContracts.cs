namespace Xplore.Contracts.Routes;

/// <summary>
/// Request to create a new saved route.
/// </summary>
public record CreateSavedRouteRequest(
    string Name,
    string? Description,
    List<SavedRouteWaypointRequest> Waypoints);

/// <summary>
/// A single waypoint in a route creation request.
/// </summary>
public record SavedRouteWaypointRequest(
    string PlaceId,
    string Name,
    double Latitude,
    double Longitude,
    int OrderIndex);

/// <summary>
/// Response with saved route information.
/// </summary>
public record SavedRouteResponse(
    Guid Id,
    string UserId,
    string Name,
    string? Description,
    bool IsCompleted,
    string ShareToken,
    DateTime CreatedAt,
    List<SavedRouteWaypointResponse> Waypoints);

/// <summary>
/// Response with a single waypoint.
/// </summary>
public record SavedRouteWaypointResponse(
    Guid Id,
    string PlaceId,
    string Name,
    double Latitude,
    double Longitude,
    int OrderIndex);
