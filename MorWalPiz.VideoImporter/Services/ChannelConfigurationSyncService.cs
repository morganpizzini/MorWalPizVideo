using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MorWalPiz.VideoImporter.Data;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.Contracts.Contracts;

namespace MorWalPiz.VideoImporter.Services;

public enum ChannelConfigurationSyncState
{
    Unknown,
    Synchronizing,
    Synchronized,
    LocalOnly
}

public sealed record ChannelConfigurationSyncStatus(
    ChannelConfigurationSyncState State,
    string ChannelId,
    string Message);

public sealed record ChannelConfigurationSaveResult(
    bool RemoteSaved,
    bool LocalCacheSynchronized,
    string Message);

public interface IChannelConfigurationSyncService
{
    ChannelConfigurationSyncStatus CurrentStatus { get; }
    bool IsSynchronizedFor(string channelId);
    bool CanAttemptSaveFor(string channelId);
    Task<ChannelConfigurationSyncStatus> SynchronizeAsync(string channelId, CancellationToken cancellationToken = default);
    Task<ChannelConfigurationSnapshot> GetLocalSnapshotAsync(CancellationToken cancellationToken = default);
    Task<ChannelConfigurationSaveResult> SaveAsync(
        string channelId,
        ChannelConfigurationSnapshot snapshot,
        CancellationToken cancellationToken = default);
}

public sealed class ChannelConfigurationSyncService(
    DatabaseService databaseService,
    ITenantContext tenantContext,
    ApiSettings apiSettings,
    IApiServiceFactory apiServiceFactory,
    ILogger<ChannelConfigurationSyncService> logger) : IChannelConfigurationSyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _syncGate = new(1, 1);
    private LastSynchronizedSelection? _lastSynchronizedSelection;
    private bool _cacheMayBeStale;
    private ChannelConfigurationSyncStatus _currentStatus = new(
        ChannelConfigurationSyncState.Unknown,
        string.Empty,
        "Le impostazioni del canale non sono ancora state sincronizzate.");

    public ChannelConfigurationSyncStatus CurrentStatus => Volatile.Read(ref _currentStatus);

    public bool IsSynchronizedFor(string channelId) =>
        CurrentStatus.State == ChannelConfigurationSyncState.Synchronized &&
        string.Equals(CurrentStatus.ChannelId, channelId, StringComparison.Ordinal);

    public bool CanAttemptSaveFor(string channelId)
    {
        if (IsSynchronizedFor(channelId))
            return true;

        var lastSynchronized = Volatile.Read(ref _lastSynchronizedSelection);
        return CurrentStatus.State == ChannelConfigurationSyncState.LocalOnly &&
            !Volatile.Read(ref _cacheMayBeStale) &&
            lastSynchronized is not null &&
            lastSynchronized.TenantId == tenantContext.CurrentTenantId &&
            string.Equals(lastSynchronized.ChannelId, channelId, StringComparison.Ordinal);
    }

    public async Task<ChannelConfigurationSyncStatus> SynchronizeAsync(
        string channelId,
        CancellationToken cancellationToken = default)
    {
        await _syncGate.WaitAsync(cancellationToken);
        var tenantId = tenantContext.CurrentTenantId;
        SetStatus(ChannelConfigurationSyncState.Synchronizing, channelId,
            "Sincronizzazione delle impostazioni del canale in corso...");

        try
        {
            EnsureChannelId(channelId);
            var api = apiServiceFactory.Create(apiSettings.ApiEndpoint, apiSettings.ApiKey, channelId);
            var remote = await api.GetVideoImporterChannelSettingsAsync(cancellationToken);
            if (remote is not null)
            {
                var remoteSnapshot = DeserializeAndValidate(remote, channelId);
                EnsureSelectionIsCurrent(tenantId, channelId);
                try
                {
                    await ApplySnapshotAsync(tenantId, remoteSnapshot, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    Volatile.Write(ref _cacheMayBeStale, true);
                    throw;
                }
                return SetStatus(ChannelConfigurationSyncState.Synchronized, channelId,
                    "Impostazioni del canale sincronizzate.");
            }

            var localSnapshot = await ReadLocalSnapshotAsync(tenantId, cancellationToken);
            _ = NormalizeAndValidate(localSnapshot);
            EnsureSelectionIsCurrent(tenantId, channelId);
            var saved = await api.SaveVideoImporterChannelSettingsAsync(
                ToRequest(localSnapshot),
                cancellationToken);
            var acknowledgedSnapshot = DeserializeAndValidate(saved, channelId);

            try
            {
                EnsureSelectionIsCurrent(tenantId, channelId);
                await ApplySnapshotAsync(tenantId, acknowledgedSnapshot, cancellationToken);
                return SetStatus(ChannelConfigurationSyncState.Synchronized, channelId,
                    "Impostazioni del canale sincronizzate.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception applyException)
            {
                logger.LogError(applyException,
                    "The initial channel settings seed succeeded, but the local cache projection failed for channel {ChannelId}",
                    channelId);
                var recovered = await TryRecoverLocalCacheAsync(api, tenantId, channelId, cancellationToken);
                if (!recovered)
                    Volatile.Write(ref _cacheMayBeStale, true);
                return SetStatus(
                    recovered ? ChannelConfigurationSyncState.Synchronized : ChannelConfigurationSyncState.LocalOnly,
                    channelId,
                    recovered
                        ? "Impostazioni del canale sincronizzate dopo il ripristino della cache locale."
                        : "Le impostazioni sono state salvate sul server, ma la cache locale non è aggiornata.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (exception is InvalidDataException or ChannelConfigurationValidationException)
                Volatile.Write(ref _cacheMayBeStale, true);
            logger.LogWarning(exception,
                "Channel settings synchronization failed for channel {ChannelId}; the local cache remains in use",
                channelId);
            return SetStatus(ChannelConfigurationSyncState.LocalOnly, channelId,
                "Sincronizzazione non disponibile. È in uso la cache locale; ogni modifica viene inviata al server prima di aggiornare la cache.");
        }
        finally
        {
            _syncGate.Release();
        }
    }

    public async Task<ChannelConfigurationSnapshot> GetLocalSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        await _syncGate.WaitAsync(cancellationToken);
        try
        {
            return await ReadLocalSnapshotAsync(tenantContext.CurrentTenantId, cancellationToken);
        }
        finally
        {
            _syncGate.Release();
        }
    }

    public async Task<ChannelConfigurationSaveResult> SaveAsync(
        string channelId,
        ChannelConfigurationSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        await _syncGate.WaitAsync(cancellationToken);
        var tenantId = tenantContext.CurrentTenantId;
        var remoteWriteAcknowledged = false;
        try
        {
            EnsureChannelId(channelId);
            if (!CanAttemptSaveFor(channelId))
                throw new InvalidOperationException("Sincronizza il canale selezionato prima di salvare le impostazioni.");
            EnsureSelectionIsCurrent(tenantId, channelId);
            var normalizedSnapshot = NormalizeAndValidate(snapshot);
            var payload = JsonSerializer.SerializeToElement(normalizedSnapshot, JsonOptions);
            var request = new VideoImporterChannelSettingsRequest(
                normalizedSnapshot.SchemaVersion,
                payload);
            var api = apiServiceFactory.Create(apiSettings.ApiEndpoint, apiSettings.ApiKey, channelId);
            var saved = await api.SaveVideoImporterChannelSettingsAsync(request, cancellationToken);
            remoteWriteAcknowledged = true;
            var acknowledgedSnapshot = DeserializeAndValidate(saved, channelId);

            try
            {
                EnsureSelectionIsCurrent(tenantId, channelId);
                await ApplySnapshotAsync(tenantId, acknowledgedSnapshot, cancellationToken);
                SetStatus(ChannelConfigurationSyncState.Synchronized, channelId,
                    "Impostazioni salvate e sincronizzate.");
                return new(true, true, "Impostazioni salvate e sincronizzate.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception applyException)
            {
                logger.LogError(applyException,
                    "Channel settings were saved remotely, but the local cache projection failed for channel {ChannelId}",
                    channelId);
                var recovered = await TryRecoverLocalCacheAsync(api, tenantId, channelId, cancellationToken);
                if (!recovered)
                    Volatile.Write(ref _cacheMayBeStale, true);
                var message = recovered
                    ? "Impostazioni salvate e cache locale ripristinata."
                    : "Impostazioni salvate sul server; la cache locale richiede una nuova sincronizzazione.";
                SetStatus(
                    recovered ? ChannelConfigurationSyncState.Synchronized : ChannelConfigurationSyncState.LocalOnly,
                    channelId,
                    message);
                return new(true, recovered, message);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (remoteWriteAcknowledged)
                Volatile.Write(ref _cacheMayBeStale, true);
            SetStatus(ChannelConfigurationSyncState.LocalOnly, channelId,
                "Salvataggio non riuscito. La cache locale non è stata modificata; riprova quando il server è disponibile.");
            logger.LogWarning(exception, "Remote-first channel settings save failed for channel {ChannelId}", channelId);
            throw;
        }
        finally
        {
            _syncGate.Release();
        }
    }

    private async Task<ChannelConfigurationSnapshot> ReadLocalSnapshotAsync(
        int tenantId,
        CancellationToken cancellationToken)
    {
        using var context = databaseService.CreateContext();
        var settings = await context.Settings.SingleOrDefaultAsync(cancellationToken);
        var languages = await context.Languages
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);
        var disclaimers = await context.Disclaimers
            .ToListAsync(cancellationToken);
        var disclaimerByLanguage = disclaimers
            .GroupBy(item => item.LanguageId)
            .ToDictionary(group => group.Key, group => group.First().Text);
        var languageSnapshot = languages.Select(language => new ChannelLanguageSnapshot(
            NormalizeCode(language.Code),
            language.Name.Trim(),
            language.IsDefault,
            language.IsSelected,
            disclaimerByLanguage.GetValueOrDefault(language.Id))).ToArray();

        if (languageSnapshot.Length == 0)
        {
            languageSnapshot =
            [
                new ChannelLanguageSnapshot("it", "Italiano", true, true, null)
            ];
        }

        var schedules = await context.PublishSchedules
            .OrderBy(item => item.Id)
            .Select(item => new ChannelPublishScheduleSnapshot(
                item.Name.Trim(),
                item.DaysOfWeek,
                item.PublishTime,
                item.IsActive))
            .ToListAsync(cancellationToken);

        return NormalizeAndValidate(new ChannelConfigurationSnapshot
        {
            ApplicationName = settings?.ApplicationName ?? string.Empty,
            DefaultHashtags = settings?.DefaultHashtags ?? string.Empty,
            Languages = languageSnapshot,
            PublishSchedules = schedules
        });
    }

    private async Task ApplySnapshotAsync(
        int tenantId,
        ChannelConfigurationSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        snapshot = NormalizeAndValidate(snapshot);
        using var context = databaseService.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var settings = await context.Settings.SingleOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new Settings { TenantId = tenantId };
            context.Settings.Add(settings);
        }
        settings.ApplicationName = snapshot.ApplicationName;
        settings.DefaultHashtags = snapshot.DefaultHashtags;

        var existingDisclaimers = await context.Disclaimers.ToListAsync(cancellationToken);
        var existingLanguages = await context.Languages.ToListAsync(cancellationToken);
        var existingSchedules = await context.PublishSchedules.ToListAsync(cancellationToken);
        context.Disclaimers.RemoveRange(existingDisclaimers);
        context.Languages.RemoveRange(existingLanguages);
        context.PublishSchedules.RemoveRange(existingSchedules);
        await context.SaveChangesAsync(cancellationToken);

        var projectedLanguages = snapshot.Languages.Select(language =>
        {
            var entity = new Language
            {
                Code = language.Code,
                Name = language.Name,
                IsDefault = language.IsDefault,
                IsSelected = language.IsSelected,
                TenantId = tenantId
            };
            if (!string.IsNullOrEmpty(language.DisclaimerText))
            {
                entity.Disclaimers =
                [
                    new Disclaimer
                    {
                        Text = language.DisclaimerText,
                        TenantId = tenantId
                    }
                ];
            }

            return entity;
        }).ToArray();
        context.Languages.AddRange(projectedLanguages);
        context.PublishSchedules.AddRange(snapshot.PublishSchedules.Select(schedule => new PublishSchedule
        {
            Name = schedule.Name,
            DaysOfWeek = schedule.DaysOfWeek,
            PublishTime = schedule.PublishTime,
            IsActive = schedule.IsActive,
            CreatedDate = DateTime.Now,
            TenantId = tenantId
        }));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<bool> TryRecoverLocalCacheAsync(
        ApiService api,
        int tenantId,
        string channelId,
        CancellationToken cancellationToken)
    {
        try
        {
            EnsureSelectionIsCurrent(tenantId, channelId);
            var remote = await api.GetVideoImporterChannelSettingsAsync(cancellationToken);
            if (remote is null)
                throw new InvalidDataException("The saved channel settings document was not returned after a successful write.");

            var snapshot = DeserializeAndValidate(remote, channelId);
            EnsureSelectionIsCurrent(tenantId, channelId);
            await ApplySnapshotAsync(tenantId, snapshot, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception recoveryException)
        {
            logger.LogError(recoveryException,
                "Could not recover the local channel settings cache for channel {ChannelId}",
                channelId);
            return false;
        }
    }

    private static VideoImporterChannelSettingsRequest ToRequest(ChannelConfigurationSnapshot snapshot) =>
        new(snapshot.SchemaVersion, JsonSerializer.SerializeToElement(snapshot, JsonOptions));

    private static ChannelConfigurationSnapshot DeserializeAndValidate(
        VideoImporterChannelSettingsResponse response,
        string requestedChannelId)
    {
        if (!string.Equals(response.ChannelId, requestedChannelId, StringComparison.Ordinal))
            throw new InvalidDataException("BackOffice returned settings for a different channel.");
        if (response.SchemaVersion != ChannelConfigurationSnapshot.CurrentSchemaVersion)
            throw new InvalidDataException("BackOffice returned an unsupported settings schema version.");
        if (response.Payload.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("BackOffice returned a non-object settings payload.");

        var snapshot = response.Payload.Deserialize<ChannelConfigurationSnapshot>(JsonOptions)
            ?? throw new InvalidDataException("BackOffice returned an empty settings payload.");
        if (snapshot.SchemaVersion != response.SchemaVersion)
            throw new InvalidDataException("The settings payload and response schema versions do not match.");
        return NormalizeAndValidate(snapshot);
    }

    private static ChannelConfigurationSnapshot NormalizeAndValidate(ChannelConfigurationSnapshot snapshot)
    {
        if (snapshot is null)
            throw new ChannelConfigurationValidationException("Settings are required.");
        if (snapshot.SchemaVersion != ChannelConfigurationSnapshot.CurrentSchemaVersion)
            throw new ChannelConfigurationValidationException("The settings schema version is not supported.");
        if (snapshot.ApplicationName is null || snapshot.ApplicationName.Length > 500)
            throw new ChannelConfigurationValidationException("The application name is invalid.");
        if (snapshot.DefaultHashtags is null || snapshot.DefaultHashtags.Length > 10_000)
            throw new ChannelConfigurationValidationException("The default hashtags are invalid.");
        if (snapshot.Languages is null ||
            snapshot.Languages.Count is < 1 or > ChannelConfigurationSnapshot.MaxLanguageCount)
            throw new ChannelConfigurationValidationException("The language list is outside the supported limits.");
        if (snapshot.PublishSchedules is null ||
            snapshot.PublishSchedules.Count > ChannelConfigurationSnapshot.MaxScheduleCount)
            throw new ChannelConfigurationValidationException("The publish schedule list exceeds the supported limit.");

        var languages = snapshot.Languages.Select(language =>
        {
            if (language is null || string.IsNullOrWhiteSpace(language.Code) ||
                string.IsNullOrWhiteSpace(language.Name))
                throw new ChannelConfigurationValidationException("Every language requires a code and display name.");
            var code = NormalizeCode(language.Code);
            var name = language.Name.Trim();
            if (code.Length > 10 || name.Length > 50 ||
                (language.DisclaimerText?.Length ?? 0) > 20_000)
                throw new ChannelConfigurationValidationException("A language setting exceeds the supported size.");
            return language with { Code = code, Name = name };
        }).ToArray();

        if (languages.Count(language => language.IsDefault) != 1)
            throw new ChannelConfigurationValidationException("Exactly one default language is required.");
        if (languages.Select(language => language.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != languages.Length)
            throw new ChannelConfigurationValidationException("Language codes must be unique.");
        if (languages.Select(language => language.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != languages.Length)
            throw new ChannelConfigurationValidationException("Language names must be unique.");

        var schedules = snapshot.PublishSchedules.Select(schedule =>
        {
            if (schedule is null || string.IsNullOrWhiteSpace(schedule.Name))
                throw new ChannelConfigurationValidationException("Every publish schedule requires a name.");
            var name = schedule.Name.Trim();
            if (name.Length > 200 || schedule.DaysOfWeek is <= 0 or > 127 ||
                schedule.PublishTime < TimeSpan.Zero || schedule.PublishTime >= TimeSpan.FromDays(1))
                throw new ChannelConfigurationValidationException("A publish schedule is invalid.");
            return schedule with { Name = name };
        }).ToArray();
        if (schedules.Select(schedule => schedule.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count() != schedules.Length)
            throw new ChannelConfigurationValidationException("Publish schedule names must be unique.");

        var normalized = snapshot with { Languages = languages, PublishSchedules = schedules };
        if (Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(normalized, JsonOptions)) >
            ChannelConfigurationSnapshot.MaxPayloadBytes)
            throw new ChannelConfigurationValidationException("The settings payload exceeds the maximum supported size.");
        return normalized;
    }

    private void EnsureSelectionIsCurrent(int expectedTenantId, string channelId)
    {
        if (tenantContext.CurrentTenantId != expectedTenantId)
            throw new ChannelSelectionChangedException();

        using var context = databaseService.CreateContext();
        var selectedChannelId = context.Tenants
            .Where(tenant => tenant.Id == expectedTenantId)
            .Select(tenant => tenant.ChannelId)
            .SingleOrDefault();
        if (string.IsNullOrWhiteSpace(selectedChannelId))
            selectedChannelId = apiSettings.ChannelId;
        if (!string.Equals(selectedChannelId, channelId, StringComparison.Ordinal))
            throw new ChannelSelectionChangedException();
    }

    private static void EnsureChannelId(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId))
            throw new ChannelConfigurationValidationException("A selected channel is required.");
    }

    private static string NormalizeCode(string code) => code.Trim().ToLowerInvariant();

    private ChannelConfigurationSyncStatus SetStatus(
        ChannelConfigurationSyncState state,
        string channelId,
        string message)
    {
        if (state == ChannelConfigurationSyncState.Synchronized)
        {
            Volatile.Write(ref _lastSynchronizedSelection,
                new LastSynchronizedSelection(tenantContext.CurrentTenantId, channelId));
            Volatile.Write(ref _cacheMayBeStale, false);
        }
        var status = new ChannelConfigurationSyncStatus(state, channelId, message);
        Volatile.Write(ref _currentStatus, status);
        return status;
    }

    private sealed record LastSynchronizedSelection(int TenantId, string ChannelId);
}

public sealed class ChannelConfigurationValidationException(string message) : Exception(message);

public sealed class ChannelSelectionChangedException()
    : InvalidOperationException("The selected channel changed during settings synchronization.");
