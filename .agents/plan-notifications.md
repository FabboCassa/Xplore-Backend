# Implementation Plan: Push Notification System — Backend (Xplore)

## Status Key
- ✅ DONE — implemented
- ✅ DONE — not started

**Current status: ✅ All phases implemented. Pending: EF migration + Firebase credentials.**

---

## Phase 1: Device Token Registration ✅ DONE

### 1.1 New Domain Entity — `UserDeviceToken`

File: `src/Xplore.Domain/Entities/UserDeviceToken.cs`

```csharp
public class UserDeviceToken
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string UserId { get; init; } = null!;
    public string Token { get; init; } = null!;         // FCM registration token
    public string Platform { get; init; } = null!;      // "android" | "ios"
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### 1.2 DbContext update

File: `src/Xplore.Infrastructure/Persistence/ApplicationDbContext.cs`

- Add `DbSet<UserDeviceToken> UserDeviceTokens`
- Configure primary key `Id`
- Unique index on `(UserId, Token)` — prevents duplicates
- Index on `UserId` — fast lookup

### 1.3 EF Migration

```bash
dotnet ef migrations add AddUserDeviceTokens \
  --project src/Xplore.Infrastructure \
  --startup-project src/Xplore.API
```

### 1.4 New API Contract

File: `src/Xplore.Contracts/RegisterDeviceTokenRequest.cs`

```csharp
public record RegisterDeviceTokenRequest(string Token, string Platform);
```

### 1.5 New Controller endpoint

File: `src/Xplore.API/Controllers/NotificationsController.cs`

```
POST /api/notifications/device-token
Authorization: Bearer <token>
Body: { "token": "<fcm_token>", "platform": "android"|"ios" }
Response: 204 No Content
```

Logic: Get current user ID from claims → Upsert token (if `(UserId, Token)` exists → update `UpdatedAt`; else insert).

---

## Phase 2: Push Notification Service ✅ DONE

### 2.1 Add Firebase Admin SDK

File: `src/Xplore.Infrastructure/Xplore.Infrastructure.csproj`

```xml
<PackageReference Include="FirebaseAdmin" Version="3.*" />
```

### 2.2 Service interface

File: `src/Xplore.Infrastructure/Notifications/IPushNotificationService.cs`

```csharp
public interface IPushNotificationService
{
    Task SendToUserAsync(string userId, string title, string body,
                         Dictionary<string, string>? data = null,
                         CancellationToken ct = default);
}
```

### 2.3 Firebase implementation

File: `src/Xplore.Infrastructure/Notifications/FirebasePushNotificationService.cs`

```csharp
public class FirebasePushNotificationService : IPushNotificationService
{
    // Constructor: inject ApplicationDbContext, ILogger
    // 1. Query UserDeviceTokens by userId
    // 2. Build MulticastMessage with notification + data
    // 3. Call FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(...)
    // 4. Log failures; remove expired/invalid tokens from DB
}
```

### 2.4 Firebase initialization

File: `src/Xplore.Infrastructure/DependencyInjection.cs`

```csharp
if (FirebaseApp.DefaultInstance == null)
{
    var credential = GoogleCredential.FromJson(config["Firebase:ServiceAccountJson"]);
    FirebaseApp.Create(new AppOptions { Credential = credential });
}
services.AddScoped<IPushNotificationService, FirebasePushNotificationService>();
```

---

## Phase 3: Notification Event Contracts ✅ DONE

File: `src/Xplore.Contracts/` — add four new event records:

```csharp
public record FriendRequestReceivedEvent(
    Guid FriendshipId,
    string RequesterId,
    string RequesterDisplayName,
    string AddresseeId);

public record AchievementReachedEvent(
    string UserId,
    int NewLevel,
    int TotalScore);

public record GroupInviteReceivedEvent(
    Guid InviteId,
    Guid GroupId,
    string GroupName,
    string InviterId,
    string InviterDisplayName,
    string InviteeId);

public record CompetitionEndedEvent(
    Guid CompetitionId,
    string CompetitionName,
    Guid GroupId,
    List<string> MemberUserIds);
```

---

## Phase 4: ExplorerLevel Domain Value Object ✅ DONE

File: `src/Xplore.Domain/ValueObjects/ExplorerLevel.cs`

```csharp
public static class ExplorerLevel
{
    private static readonly int[] Thresholds = [0, 50, 150, 300, 500, 800, 1200, 1800, 2600, 3500];

    public static int LevelForPoints(int points) =>
        Thresholds.Count(t => points >= t);
}
```

---

## Phase 5: Publish Events from Existing Controllers ✅ DONE

### 5.1 Friend Request event

File: `src/Xplore.API/Controllers/FriendsController.cs`
Method: `SendFriendRequest` (POST `/api/friends/request`)

After saving the `Friendship` entity, inject `IPublishEndpoint` and publish:
```csharp
await publishEndpoint.Publish(new FriendRequestReceivedEvent(
    friendship.Id, currentUserId, currentUser.DisplayName, request.AddresseeId), ct);
```

### 5.2 Achievement/Level event

File: `src/Xplore.API/Controllers/CommunityController.cs`
Method: `VisitPlace` (POST `/api/community/visits`)

After updating `TotalScore`:
- Calculate level before/after using `ExplorerLevel.LevelForPoints()`
- If level increased → publish `AchievementReachedEvent`

### 5.3 Competition Ended event

File: `src/Xplore.API/Controllers/CommunityController.cs`
Method: `UpdateCompetition` (PUT `/api/community/groups/{groupId}/competitions/{compId}`)

When `IsActive` changes `true → false`:
- Load all group member user IDs
- Publish `CompetitionEndedEvent`

---

## Phase 6: Group Invitation System ✅ DONE

### 6.1 New Domain Entity — `GroupInvite`

File: `src/Xplore.Domain/Entities/GroupInvite.cs`

```csharp
public class GroupInvite
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid GroupId { get; init; }
    public string InviterId { get; init; } = null!;
    public string InviteeId { get; init; } = null!;
    public GroupInviteStatus Status { get; set; } = GroupInviteStatus.Pending;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
    public Group Group { get; init; } = null!;
}

public enum GroupInviteStatus { Pending, Accepted, Rejected }
```

### 6.2 DbContext + Migration

- Add `DbSet<GroupInvite> GroupInvites`
- Unique index on `(GroupId, InviteeId)` where `Status = Pending`
- Migration: `AddGroupInvites`

### 6.3 API Contracts

```csharp
public record SendGroupInviteRequest(string InviteeUserId);
public record GroupInviteResponse(
    Guid Id, Guid GroupId, string GroupName,
    string InvitedByUserId, string? InvitedByDisplayName,
    string InvitedUserId, int Status, DateTime CreatedAt);
```

### 6.4 New CommunityController endpoints

```
POST /api/community/groups/{groupId}/invite
Authorization: Bearer (admin only)
Body: { "inviteeUserId": "..." }
→ Creates GroupInvite, publishes GroupInviteReceivedEvent

GET /api/community/invites
Authorization: Bearer
→ Returns List<GroupInviteResponse> (pending invites for current user)

POST /api/community/invites/{inviteId}/accept
Authorization: Bearer
→ Creates GroupMember, sets Status = Accepted

POST /api/community/invites/{inviteId}/reject
Authorization: Bearer
→ Sets Status = Rejected
```

---

## Phase 7: Notification Consumers (Xplore.Worker) ✅ DONE

### 7.1 Four consumer files

```
src/Xplore.Worker/Consumers/FriendRequestNotificationConsumer.cs
src/Xplore.Worker/Consumers/AchievementNotificationConsumer.cs
src/Xplore.Worker/Consumers/GroupInviteNotificationConsumer.cs
src/Xplore.Worker/Consumers/CompetitionEndedNotificationConsumer.cs
```

Each: inject `IPushNotificationService`, call `SendToUserAsync`.

**Notification copy:**

| Event | Title | Body |
|-------|-------|------|
| FriendRequestReceived | "Nuova richiesta di amicizia" | "{DisplayName} vuole essere tuo amico" |
| AchievementReached | "Livello raggiunto! 🎉" | "Sei al livello {NewLevel}! Continua così" |
| GroupInviteReceived | "Invito al gruppo" | "{InviterName} ti ha invitato in {GroupName}" |
| CompetitionEnded | "Competizione terminata" | "La competizione '{Name}' è finita. Vedi i risultati!" |

### 7.2 Register consumers in `Xplore.Worker/Program.cs`

```csharp
cfg.AddConsumer<FriendRequestNotificationConsumer>();
cfg.AddConsumer<AchievementNotificationConsumer>();
cfg.AddConsumer<GroupInviteNotificationConsumer>();
cfg.AddConsumer<CompetitionEndedNotificationConsumer>();
```

---

## Phase 8: Scheduled Competition Expiry Job ✅ DONE

File: `src/Xplore.Worker/Jobs/CompetitionExpiryJob.cs`

Use `IHostedService` (or Quartz.NET):
- Runs every hour
- Queries competitions where `EndDate <= now AND IsActive = true`
- Sets `IsActive = false`, loads member IDs, publishes `CompetitionEndedEvent`

---

## Phase 9: Configuration ✅ DONE

`appsettings.json`:
```json
"Firebase": {
  "ServiceAccountJson": ""
}
```

Development: `dotnet user-secrets set "Firebase:ServiceAccountJson" "..."`
Production (.NET Aspire): inject as environment variable.

---

## Implementation Order

1. `UserDeviceToken` entity + migration + `POST /api/notifications/device-token` controller
2. Firebase Admin SDK + `IPushNotificationService` interface + `FirebasePushNotificationService`
3. Four event contracts in `Xplore.Contracts`
4. `ExplorerLevel` domain value object
5. Publish events from `FriendsController`, `CommunityController`
6. `GroupInvite` entity + migration + four controller endpoints
7. Four notification consumers in `Xplore.Worker`
8. Scheduled `CompetitionExpiryJob`
9. Firebase config in `appsettings.json` + secrets setup

---

## Files to Create

| File | Purpose |
|------|---------|
| `Xplore.Domain/Entities/UserDeviceToken.cs` | Device token entity |
| `Xplore.Domain/Entities/GroupInvite.cs` | Group invite entity |
| `Xplore.Domain/ValueObjects/ExplorerLevel.cs` | Level thresholds |
| `Xplore.Infrastructure/Notifications/IPushNotificationService.cs` | Interface |
| `Xplore.Infrastructure/Notifications/FirebasePushNotificationService.cs` | Firebase impl |
| `Xplore.Contracts/FriendRequestReceivedEvent.cs` | Event |
| `Xplore.Contracts/AchievementReachedEvent.cs` | Event |
| `Xplore.Contracts/GroupInviteReceivedEvent.cs` | Event |
| `Xplore.Contracts/CompetitionEndedEvent.cs` | Event |
| `Xplore.Contracts/RegisterDeviceTokenRequest.cs` | DTO |
| `Xplore.Contracts/SendGroupInviteRequest.cs` | DTO |
| `Xplore.Contracts/GroupInviteResponse.cs` | DTO |
| `Xplore.API/Controllers/NotificationsController.cs` | Device token endpoint |
| `Xplore.Worker/Consumers/FriendRequestNotificationConsumer.cs` | Consumer |
| `Xplore.Worker/Consumers/AchievementNotificationConsumer.cs` | Consumer |
| `Xplore.Worker/Consumers/GroupInviteNotificationConsumer.cs` | Consumer |
| `Xplore.Worker/Consumers/CompetitionEndedNotificationConsumer.cs` | Consumer |
| `Xplore.Worker/Jobs/CompetitionExpiryJob.cs` | Scheduled job |

## Files to Modify

| File | Change |
|------|--------|
| `ApplicationDbContext.cs` | Add `UserDeviceTokens` + `GroupInvites` DbSets, configure indexes |
| `Infrastructure/DependencyInjection.cs` | Register Firebase + `IPushNotificationService` |
| `FriendsController.cs` | Inject `IPublishEndpoint`, publish `FriendRequestReceivedEvent` |
| `CommunityController.cs` | Publish Achievement + CompetitionEnded events; add GroupInvite endpoints |
| `Xplore.Worker/Program.cs` | Register 4 notification consumers |
| `appsettings.json` | Add Firebase config key |
