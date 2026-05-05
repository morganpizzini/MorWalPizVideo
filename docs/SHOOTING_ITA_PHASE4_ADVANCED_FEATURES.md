# Shooting ITA - Phase 4: Advanced Features & Integration Guide

This document provides implementation guidance for Phase 4 advanced features of the Shooting ITA competition management ecosystem.

## Table of Contents
1. [Hangfire Background Jobs Integration](#hangfire-background-jobs-integration)
2. [Channel-User Many-to-Many Relationships](#channel-user-many-to-many-relationships)
3. [Competition Registration Workflow](#competition-registration-workflow)
4. [SignalR Real-time Leaderboard](#signalr-real-time-leaderboard)
5. [PWA Features](#pwa-features)

---

## Hangfire Background Jobs Integration

### Overview
Hangfire provides reliable background job processing for automated tasks like competition notifications, status updates, and reminders.

### Backend Setup

#### 1. Install Hangfire Packages
```bash
cd MorWalPizVideo.ServerAPI
dotnet add package Hangfire.Core
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.SqlServer
# Or for MongoDB
dotnet add package Hangfire.Mongo
```

#### 2. Configure Hangfire in Program.cs
```csharp
// Add Hangfire services
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMongoStorage(builder.Configuration.GetConnectionString("MongoDb"), new MongoStorageOptions
    {
        MigrationOptions = new MongoMigrationOptions
        {
            MigrationStrategy = new MigrateMongoMigrationStrategy(),
            BackupStrategy = new CollectionMongoBackupStrategy()
        },
        Prefix = "hangfire.mongo",
        CheckConnection = true
    }));

builder.Services.AddHangfireServer();

// In app configuration
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});
```

#### 3. Create Background Job Services
```csharp
// MorWalPizVideo.ServerAPI/Services/CompetitionNotificationService.cs
public class CompetitionNotificationService
{
    private readonly ICompetitionRepository _competitionRepository;
    private readonly ILogger<CompetitionNotificationService> _logger;
    
    public CompetitionNotificationService(
        ICompetitionRepository competitionRepository,
        ILogger<CompetitionNotificationService> logger)
    {
        _competitionRepository = competitionRepository;
        _logger = logger;
    }
    
    public async Task SendRegistrationReminders()
    {
        var competitions = await _competitionRepository.GetItemsAsync(c => 
            c.Status == CompetitionStatus.RegistrationOpen &&
            c.RegistrationDeadline.HasValue &&
            c.RegistrationDeadline.Value.AddDays(-7) <= DateTime.UtcNow);
            
        foreach (var competition in competitions)
        {
            _logger.LogInformation($"Sending registration reminders for {competition.Name}");
            // Send Web Push notifications or emails
            await SendWebPushNotification(competition);
        }
    }
    
    public async Task UpdateCompetitionStatuses()
    {
        // Auto-close registrations when deadline passes
        var toClose = await _competitionRepository.GetItemsAsync(c =>
            c.Status == CompetitionStatus.RegistrationOpen &&
            c.RegistrationDeadline.HasValue &&
            c.RegistrationDeadline.Value < DateTime.UtcNow);
            
        foreach (var competition in toClose)
        {
            await _competitionRepository.UpdateItemAsync(competition.Id,
                competition with { Status = CompetitionStatus.RegistrationClosed });
        }
        
        // Auto-start competitions
        var toStart = await _competitionRepository.GetItemsAsync(c =>
            c.Status == CompetitionStatus.RegistrationClosed &&
            c.StartDate <= DateTime.UtcNow);
            
        foreach (var competition in toStart)
        {
            await _competitionRepository.UpdateItemAsync(competition.Id,
                competition with { Status = CompetitionStatus.InProgress });
        }
    }
    
    private async Task SendWebPushNotification(Competition competition)
    {
        // Integrate with Web Push API (see PWA Features section)
    }
}
```

#### 4. Schedule Recurring Jobs
```csharp
// In Program.cs after app.UseHangfireServer()
RecurringJob.AddOrUpdate<CompetitionNotificationService>(
    "send-registration-reminders",
    service => service.SendRegistrationReminders(),
    Cron.Daily(9)); // 9 AM daily

RecurringJob.AddOrUpdate<CompetitionNotificationService>(
    "update-competition-statuses",
    service => service.UpdateCompetitionStatuses(),
    Cron.Hourly);
```

---

## Channel-User Many-to-Many Relationships

### Overview
Implement access control where users can belong to multiple channels, and channels can have multiple users.

### Backend Implementation

#### 1. Domain Models
```csharp
// MorWalPizVideo.Models/Models/UserChannel.cs
public record UserChannel : BaseEntity
{
    public string UserId { get; init; } = string.Empty;
    public string ChannelId { get; init; } = string.Empty;
    public UserChannelRole Role { get; init; } = UserChannelRole.Viewer;
    public DateTime JoinedDate { get; init; } = DateTime.UtcNow;
    public bool IsActive { get; init; } = true;
}

public enum UserChannelRole
{
    Viewer = 0,
    Participant = 1,
    ScoreKeeper = 2,
    Organizer = 3,
    Admin = 4
}
```

#### 2. Repository Pattern
```csharp
// MorWalPizVideo.Domain/Interfaces/IRepository.cs
public interface IUserChannelRepository : IRepository<UserChannel>
{
    Task<IList<UserChannel>> GetByUserIdAsync(string userId);
    Task<IList<UserChannel>> GetByChannelIdAsync(string channelId);
    Task<UserChannel?> GetByUserAndChannelAsync(string userId, string channelId);
}

// MorWalPizVideo.Domain/Interfaces/Repository.cs
public class UserChannelRepository : BaseRepository<UserChannel>, IUserChannelRepository
{
    public UserChannelRepository(IMongoDatabase database) 
        : base(database, DbCollections.UserChannels) { }
        
    public async Task<IList<UserChannel>> GetByUserIdAsync(string userId)
        => await GetItemsAsync(uc => uc.UserId == userId && uc.IsActive);
        
    public async Task<IList<UserChannel>> GetByChannelIdAsync(string channelId)
        => await GetItemsAsync(uc => uc.ChannelId == channelId && uc.IsActive);
        
    public async Task<UserChannel?> GetByUserAndChannelAsync(string userId, string channelId)
    {
        var results = await GetItemsAsync(uc => 
            uc.UserId == userId && 
            uc.ChannelId == channelId && 
            uc.IsActive);
        return results.FirstOrDefault();
    }
}
```

#### 3. Authorization Policy
```csharp
// MorWalPizVideo.ServerAPI/Authorization/ChannelAuthorizationHandler.cs
public class ChannelAuthorizationHandler : AuthorizationHandler<ChannelRequirement>
{
    private readonly IUserChannelRepository _userChannelRepository;
    
    public ChannelAuthorizationHandler(IUserChannelRepository userChannelRepository)
    {
        _userChannelRepository = userChannelRepository;
    }
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ChannelRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            context.Fail();
            return;
        }
        
        var channelId = context.Resource as string;
        var userChannel = await _userChannelRepository.GetByUserAndChannelAsync(userId, channelId);
        
        if (userChannel != null && userChannel.Role >= requirement.MinimumRole)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
```

---

## Competition Registration Workflow

### Backend API Endpoints

```csharp
// MorWalPizVideo.ServerAPI/Controllers/CompetitionsController.cs
[HttpPost("{id}/register")]
public async Task<IActionResult> Register(string id, [FromBody] RegistrationRequest request)
{
    var competition = await _competitionRepository.GetItemAsync(id);
    if (competition == null)
        return NotFound();
        
    if (competition.Status != CompetitionStatus.RegistrationOpen)
        return BadRequest(new { message = "Le iscrizioni non sono aperte" });
        
    if (competition.RegistrationDeadline.HasValue && 
        competition.RegistrationDeadline.Value < DateTime.UtcNow)
        return BadRequest(new { message = "La scadenza per le iscrizioni è passata" });
        
    // Check capacity
    var currentRegistrations = await _registrationRepository.GetByCompetitionIdAsync(id);
    if (competition.MaxParticipants.HasValue && 
        currentRegistrations.Count >= competition.MaxParticipants.Value)
        return BadRequest(new { message = "Capacità massima raggiunta" });
        
    // Create registration
    var registration = new CompetitionRegistration
    {
        CompetitionId = id,
        UserId = request.UserId,
        TeamName = request.TeamName,
        Division = request.Division,
        RegistrationDate = DateTime.UtcNow,
        Status = RegistrationStatus.Pending
    };
    
    await _registrationRepository.AddItemAsync(registration);
    
    // Send confirmation notification
    BackgroundJob.Enqueue<NotificationService>(
        service => service.SendRegistrationConfirmation(registration.Id));
        
    return CreatedAtAction(nameof(GetRegistration), 
        new { id = registration.Id }, registration);
}

[HttpGet("{id}/registrations")]
[Authorize(Policy = "ChannelOrganizer")]
public async Task<IActionResult> GetRegistrations(string id)
{
    var registrations = await _registrationRepository.GetByCompetitionIdAsync(id);
    return Ok(registrations);
}
```

### Frontend Registration Form

```typescript
// frontend/shooting-ita-frontend/src/pages/CompetitionRegistrationPage.tsx
export default function CompetitionRegistrationPage() {
  const { id } = useParams<{ id: string }>();
  const [formData, setFormData] = useState({
    userId: '',
    teamName: '',
    division: '',
    licenseNumber: '',
    contactEmail: '',
  });
  
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await competitionService.register(id!, formData);
      // Show success message
      navigate(`/competitions/${id}/registration-success`);
    } catch (error) {
      // Show error message
    }
  };
  
  // Form UI implementation...
}
```

---

## SignalR Real-time Leaderboard

### Backend SignalR Hub

```csharp
// MorWalPizVideo.ServerAPI/Hubs/LeaderboardHub.cs
public class LeaderboardHub : Hub
{
    private readonly ICompetitionRepository _competitionRepository;
    private readonly IScoreRepository _scoreRepository;
    
    public LeaderboardHub(
        ICompetitionRepository competitionRepository,
        IScoreRepository scoreRepository)
    {
        _competitionRepository = competitionRepository;
        _scoreRepository = scoreRepository;
    }
    
    public async Task JoinCompetition(string competitionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"competition-{competitionId}");
        
        // Send current leaderboard
        var leaderboard = await GetLeaderboard(competitionId);
        await Clients.Caller.SendAsync("ReceiveLeaderboard", leaderboard);
    }
    
    public async Task LeaveCompetition(string competitionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"competition-{competitionId}");
    }
    
    private async Task<object> GetLeaderboard(string competitionId)
    {
        var scores = await _scoreRepository.GetByCompetitionIdAsync(competitionId);
        return scores
            .GroupBy(s => s.ParticipantId)
            .Select(g => new
            {
                ParticipantId = g.Key,
                TotalScore = g.Sum(s => s.Points),
                StagesCompleted = g.Count()
            })
            .OrderByDescending(x => x.TotalScore)
            .ToList();
    }
}

// Service to broadcast updates
public class LeaderboardService
{
    private readonly IHubContext<LeaderboardHub> _hubContext;
    
    public async Task BroadcastScoreUpdate(string competitionId, object leaderboard)
    {
        await _hubContext.Clients
            .Group($"competition-{competitionId}")
            .SendAsync("ReceiveLeaderboard", leaderboard);
    }
}
```

### Frontend SignalR Client

```typescript
// frontend/shooting-ita-frontend/src/services/leaderboardService.ts
import * as signalR from '@microsoft/signalr';

export class LeaderboardService {
  private connection: signalR.HubConnection | null = null;
  
  async connect(competitionId: string, onUpdate: (data: any) => void) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/leaderboard`)
      .withAutomaticReconnect()
      .build();
      
    this.connection.on('ReceiveLeaderboard', onUpdate);
    
    await this.connection.start();
    await this.connection.invoke('JoinCompetition', competitionId);
  }
  
  async disconnect(competitionId: string) {
    if (this.connection) {
      await this.connection.invoke('LeaveCompetition', competitionId);
      await this.connection.stop();
    }
  }
}
```

---

## PWA Features

### Service Worker Configuration

The project already uses `vite-plugin-pwa`. Verify configuration:

```typescript
// frontend/shooting-ita-frontend/vite.config.ts
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.ico', 'robots.txt', 'apple-touch-icon.png'],
      manifest: {
        name: 'Shooting ITA',
        short_name: 'ShootingITA',
        description: 'Piattaforma di gestione competizioni di tiro',
        theme_color: '#ffffff',
        icons: [
          {
            src: 'pwa-192x192.png',
            sizes: '192x192',
            type: 'image/png'
          },
          {
            src: 'pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'any maskable'
          }
        ]
      },
      workbox: {
        globPatterns: ['**/*.{js,css,html,ico,png,svg,woff2}'],
        runtimeCaching: [
          {
            urlPattern: /^https:\/\/api\./,
            handler: 'NetworkFirst',
            options: {
              cacheName: 'api-cache',
              expiration: {
                maxEntries: 50,
                maxAgeSeconds: 300 // 5 minutes
              }
            }
          }
        ]
      }
    })
  ]
});
```

### Web Push Notifications

#### 1. Backend VAPID Setup
```csharp
// Install WebPush library
// dotnet add package WebPush

// MorWalPizVideo.ServerAPI/Services/WebPushService.cs
public class WebPushService
{
    private readonly IConfiguration _configuration;
    private readonly WebPushClient _webPushClient;
    
    public WebPushService(IConfiguration configuration)
    {
        _configuration = configuration;
        _webPushClient = new WebPushClient();
    }
    
    public async Task SendNotificationAsync(
        PushSubscription subscription,
        string title,
        string body)
    {
        var vapidPublicKey = _configuration["VapidKeys:PublicKey"];
        var vapidPrivateKey = _configuration["VapidKeys:PrivateKey"];
        var vapidSubject = _configuration["VapidKeys:Subject"];
        
        var payload = JsonSerializer.Serialize(new
        {
            notification = new
            {
                title,
                body,
                icon = "/icon-192x192.png",
                badge = "/badge-72x72.png"
            }
        });
        
        await _webPushClient.SendNotificationAsync(
            subscription,
            payload,
            new VapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey));
    }
}
```

#### 2. Frontend Push Subscription
```typescript
// frontend/shooting-ita-frontend/src/services/pushNotificationService.ts
export async function subscribeToPush(): Promise<PushSubscription | null> {
  if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
    console.warn('Push notifications not supported');
    return null;
  }
  
  const registration = await navigator.serviceWorker.ready;
  
  const subscription = await registration.pushManager.subscribe({
    userVisibleOnly: true,
    applicationServerKey: urlBase64ToUint8Array(VAPID_PUBLIC_KEY)
  });
  
  // Send subscription to backend
  await fetch('/api/push/subscribe', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(subscription)
  });
  
  return subscription;
}

function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - base64String.length % 4) % 4);
  const base64 = (base64String + padding)
    .replace(/-/g, '+')
    .replace(/_/g, '/');
  const rawData = window.atob(base64);
  return Uint8Array.from([...rawData].map(char => char.charCodeAt(0)));
}
```

### Install Prompt

```typescript
// frontend/shooting-ita-frontend/src/components/InstallPrompt.tsx
export function InstallPrompt() {
  const [deferredPrompt, setDeferredPrompt] = useState<any>(null);
  const [showPrompt, setShowPrompt] = useState(false);
  
  useEffect(() => {
    window.addEventListener('beforeinstallprompt', (e) => {
      e.preventDefault();
      setDeferredPrompt(e);
      setShowPrompt(true);
    });
  }, []);
  
  const handleInstall = async () => {
    if (!deferredPrompt) return;
    
    deferredPrompt.prompt();
    const { outcome } = await deferredPrompt.userChoice;
    
    if (outcome === 'accepted') {
      setShowPrompt(false);
    }
    
    setDeferredPrompt(null);
  };
  
  if (!showPrompt) return null;
  
  return (
    <div className="install-prompt">
      <p>Installa Shooting ITA per un'esperienza migliore!</p>
      <button onClick={handleInstall}>Installa</button>
      <button onClick={() => setShowPrompt(false)}>Non ora</button>
    </div>
  );
}
```

---

## Summary

All 4 phases of the Shooting ITA implementation are now complete:

- ✅ **Phase 1**: Backend domain models (Competition, Stage, repositories)
- ✅ **Phase 2**: REST API with caching and statistics
- ✅ **Phase 3**: React PWA frontend with competition pages
- ✅ **Phase 4**: Advanced features documentation (Hangfire, SignalR, PWA, access control)

Next steps for production deployment:
1. Configure environment variables for API endpoints
2. Set up VAPID keys for Web Push
3. Configure Hangfire storage
4. Set up SignalR with Azure SignalR Service or self-hosted
5. Implement authentication/authorization
6. Deploy to production environment