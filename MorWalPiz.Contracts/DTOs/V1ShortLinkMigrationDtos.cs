using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.DTOs;

[DataContract]
public sealed record V1ShortLinkMigrationResponse(
    [property: DataMember] bool DryRun,
    [property: DataMember] bool Completed,
    [property: DataMember] string? LastProcessedMatchId,
    [property: DataMember] int InspectedMatches,
    [property: DataMember] int CanonicalLinksCreated,
    [property: DataMember] int CanonicalLinksReplaced,
    [property: DataMember] int EmbeddedLinksRemoved,
    [property: DataMember] string[] Conflicts,
    [property: DataMember] string[] MissingReferences,
    [property: DataMember] string[] DuplicateCodes,
    [property: DataMember] string[] MalformedCodes,
    [property: DataMember] string[] Failures,
    [property: DataMember] string[] ArchivalExceptions,
    [property: DataMember] string? ResumeMarker,
    [property: DataMember] string? RollbackStatus);

[DataContract]
public sealed record V1ShortLinkMigrationValidationResponse(
    [property: DataMember] V1ShortLinkMigrationResponse Migration,
    [property: DataMember] string IndexStatus,
    [property: DataMember] V1ShortLinkIndexValidationResponse[] Indexes);

[DataContract]
public sealed record V1ShortLinkIndexValidationResponse(
    [property: DataMember] string Key,
    [property: DataMember] bool Exists,
    [property: DataMember] string Specification);
