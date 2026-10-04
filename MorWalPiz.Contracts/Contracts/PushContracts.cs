using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using MorWalPizVideo.Server.Models;

namespace MorWalPiz.Contracts.Contracts;

// ---------------------------------------------------------------------------
// Public (anonymous) surface — ServerAPI. Never echoes the raw push endpoint.
// ---------------------------------------------------------------------------

[DataContract]
public sealed class PushSubscriptionKeysRequest
{
    [DataMember][Required] public string P256dh { get; set; } = string.Empty;
    [DataMember][Required] public string Auth { get; set; } = string.Empty;
}

[DataContract]
public sealed class PushSubscribeRequest
{
    [DataMember][Required][Url] public string Endpoint { get; set; } = string.Empty;
    [DataMember][Required] public PushSubscriptionKeysRequest Keys { get; set; } = new();
    [DataMember][Required] public IReadOnlyList<string> ChannelIds { get; set; } = [];
    [DataMember][Required][StringLength(64)] public string ApplicationKey { get; set; } = string.Empty;
    [DataMember][StringLength(8)] public string? Language { get; set; }

    /// <summary>Anonymous management credential issued on first consent. Required to change an existing endpoint.</summary>
    [DataMember][StringLength(128)] public string? Credential { get; set; }
}

[DataContract]
public sealed class PushSubscriptionCredentialRequest
{
    [DataMember][Required][Url] public string Endpoint { get; set; } = string.Empty;
    [DataMember][Required][StringLength(128)] public string Credential { get; set; } = string.Empty;
}

/// <summary>
/// Anonymous subscription state. <see cref="Credential"/> is returned exactly once, when it is minted.
/// The push endpoint is deliberately absent: the browser already owns it.
/// </summary>
[DataContract]
public sealed class PushSubscriptionStateContract
{
    [DataMember] public IReadOnlyList<string> ChannelIds { get; set; } = [];
    [DataMember] public string? Credential { get; set; }
}

[DataContract]
public sealed class PushPublicKeyContract
{
    [DataMember] public string PublicKey { get; set; } = string.Empty;
}

// ---------------------------------------------------------------------------
// BackOffice surface
// ---------------------------------------------------------------------------

[DataContract]
public sealed class PushAudienceContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string Code { get; set; } = string.Empty;
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public string? Description { get; set; }
    [DataMember] public IReadOnlyList<string> ChannelIds { get; set; } = [];
    [DataMember] public bool IsActive { get; set; }
    [DataMember] public DateTime CreationDateTime { get; set; }
    [DataMember] public DateTime? UpdatedAt { get; set; }
}

[DataContract]
public sealed class PushAudienceRequest
{
    [DataMember][Required][StringLength(64, MinimumLength = 2)] public string Code { get; set; } = string.Empty;
    [DataMember][Required][StringLength(120, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [DataMember][StringLength(500)] public string? Description { get; set; }
    [DataMember][Required][MinLength(1)] public IReadOnlyList<string> ChannelIds { get; set; } = [];
    [DataMember] public bool IsActive { get; set; } = true;
}

[DataContract]
public sealed class PushTargetChannelContract
{
    [DataMember] public string ChannelId { get; set; } = string.Empty;
    [DataMember] public string ChannelName { get; set; } = string.Empty;
    [DataMember] public int ActiveSubscriberCount { get; set; }
}

/// <summary>Everything the platform composer needs to offer select-all over channels and audiences.</summary>
[DataContract]
public sealed class PushTargetsContract
{
    [DataMember] public IReadOnlyList<PushTargetChannelContract> Channels { get; set; } = [];
    [DataMember] public IReadOnlyList<PushAudienceContract> Audiences { get; set; } = [];
    [DataMember] public int MaxActions { get; set; }
}

[DataContract]
public sealed class PushNotificationActionRequest
{
    [DataMember][Required][StringLength(32, MinimumLength = 1)] public string Action { get; set; } = string.Empty;
    [DataMember][Required][StringLength(60, MinimumLength = 1)] public string Title { get; set; } = string.Empty;
    [DataMember][StringLength(300)] public string? Destination { get; set; }
}

[DataContract]
public sealed class PushNotificationTemplateContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Body { get; set; } = string.Empty;
    [DataMember] public string Destination { get; set; } = "/";
    [DataMember] public IReadOnlyList<PushNotificationActionRequest> Actions { get; set; } = [];
    [DataMember] public int Version { get; set; }
    [DataMember] public bool IsActive { get; set; }
    [DataMember] public DateTime UpdatedAt { get; set; }
}

[DataContract]
public sealed class PushNotificationTemplateRequest
{
    [DataMember][Required][StringLength(100, MinimumLength = 1)] public string Name { get; set; } = string.Empty;
    [DataMember][Required][StringLength(80, MinimumLength = 1)] public string Title { get; set; } = string.Empty;
    [DataMember][Required][StringLength(300, MinimumLength = 1)] public string Body { get; set; } = string.Empty;
    [DataMember][StringLength(300)] public string? Destination { get; set; }
    [DataMember] public IReadOnlyList<PushNotificationActionRequest> Actions { get; set; } = [];
}

/// <summary>Platform-wide send. Targets are named channel collections and/or explicit channels.</summary>
[DataContract]
public sealed class PushPlatformSendRequest
{
    [DataMember][Required][StringLength(80, MinimumLength = 1)] public string Title { get; set; } = string.Empty;
    [DataMember][Required][StringLength(300, MinimumLength = 1)] public string Body { get; set; } = string.Empty;
    [DataMember][StringLength(300)] public string? Destination { get; set; }
    [DataMember] public IReadOnlyList<PushNotificationActionRequest> Actions { get; set; } = [];
    [DataMember] public IReadOnlyList<string> ChannelIds { get; set; } = [];
    [DataMember] public IReadOnlyList<string> AudienceIds { get; set; } = [];

    /// <summary>Select-all shortcut: every channel that currently has at least one active subscriber.</summary>
    [DataMember] public bool AllChannels { get; set; }
    [DataMember] public string? TemplateId { get; set; }
}

/// <summary>Channel-owner broadcast. The audience is every subscriber of the scoped channel; no per-user selection.</summary>
[DataContract]
public sealed class PushChannelSendRequest
{
    [DataMember][Required][StringLength(80, MinimumLength = 1)] public string Title { get; set; } = string.Empty;
    [DataMember][Required][StringLength(300, MinimumLength = 1)] public string Body { get; set; } = string.Empty;
    [DataMember][StringLength(300)] public string? Destination { get; set; }
    [DataMember] public IReadOnlyList<PushNotificationActionRequest> Actions { get; set; } = [];
    [DataMember] public string? TemplateId { get; set; }
}

[DataContract]
public sealed class PushDispatchContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public PushDispatchScope Scope { get; set; }
    [DataMember] public PushDispatchState State { get; set; }
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Body { get; set; } = string.Empty;
    [DataMember] public string Destination { get; set; } = string.Empty;
    [DataMember] public IReadOnlyList<PushNotificationActionRequest> Actions { get; set; } = [];
    [DataMember] public IReadOnlyList<string> ChannelIds { get; set; } = [];
    [DataMember] public IReadOnlyList<string> AudienceIds { get; set; } = [];
    [DataMember] public string? OwnerChannelId { get; set; }
    [DataMember] public int RecipientCount { get; set; }
    [DataMember] public DateTime CreationDateTime { get; set; }
    [DataMember] public DateTime? QueuedAt { get; set; }
    [DataMember] public DateTime? CompletedAt { get; set; }
}
