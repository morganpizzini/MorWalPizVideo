using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Configuration;

namespace MorWalPizVideo.BackOffice.Tests.Services;

public sealed class SocialAssetBlobServiceTests
{
    [Fact]
    public async Task Managed_identity_uses_user_delegation_sas_without_shared_key()
    {
        var handler = new UserDelegationKeyHandler();
        var client = CreateClient(handler);
        var service = new SocialAssetBlobService(
            client,
            Options.Create(new BlobStorageOptions
            {
                PreferManagedIdentity = true,
                StorageAccountName = "storageaccount",
                StorageAccountKey = "not-used",
                SocialAssetContainerName = "social-assets"
            }),
            NullLogger<SocialAssetBlobService>.Instance);

        var uri = await service.CreateReadSasAsync(
            "social/channel/asset.jpg",
            DateTimeOffset.UtcNow.AddMinutes(30));

        Assert.Contains("skoid=", uri.Query, StringComparison.Ordinal);
        Assert.Contains("sktid=", uri.Query, StringComparison.Ordinal);
        Assert.Contains("sp=r", uri.Query, StringComparison.Ordinal);
        Assert.Contains("sr=b", uri.Query, StringComparison.Ordinal);
        Assert.True(handler.UserDelegationKeyRequested);
    }

    [Fact]
    public async Task Shared_key_fallback_requires_explicit_non_managed_identity_mode()
    {
        var service = new SocialAssetBlobService(
            new BlobServiceClient(new Uri("https://storageaccount.blob.core.windows.net")),
            Options.Create(new BlobStorageOptions
            {
                PreferManagedIdentity = false,
                StorageAccountName = "storageaccount",
                StorageAccountKey = Convert.ToBase64String(new byte[32]),
                SocialAssetContainerName = "social-assets"
            }),
            NullLogger<SocialAssetBlobService>.Instance);

        var uri = await service.CreateReadSasAsync(
            "social/channel/asset.jpg",
            DateTimeOffset.UtcNow.AddMinutes(30));

        Assert.Contains("sp=r", uri.Query, StringComparison.Ordinal);
        Assert.Contains("sig=", uri.Query, StringComparison.Ordinal);
        Assert.DoesNotContain("skoid=", uri.Query, StringComparison.Ordinal);
    }

    private static BlobServiceClient CreateClient(HttpMessageHandler handler) =>
        new(
            new Uri("https://storageaccount.blob.core.windows.net"),
            new TestTokenCredential(),
            new BlobClientOptions
            {
                Transport = new HttpClientTransport(new HttpClient(handler))
            });

    private sealed class TestTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new("test-token", DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }

    private sealed class UserDelegationKeyHandler : HttpMessageHandler
    {
        public bool UserDelegationKeyRequested { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            UserDelegationKeyRequested = request.RequestUri?.Query.Contains("comp=userdelegationkey", StringComparison.OrdinalIgnoreCase) == true;
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    <?xml version="1.0" encoding="utf-8"?>
                    <UserDelegationKey>
                      <SignedOid>00000000-0000-0000-0000-000000000001</SignedOid>
                      <SignedTid>00000000-0000-0000-0000-000000000002</SignedTid>
                      <SignedStart>2026-09-18T00:00:00Z</SignedStart>
                      <SignedExpiry>2026-09-19T00:00:00Z</SignedExpiry>
                      <SignedService>b</SignedService>
                      <SignedVersion>2020-02-10</SignedVersion>
                      <Value>dGVzdC11c2VyLWRlbGVnYXRpb24ta2V5</Value>
                    </UserDelegationKey>
                    """)
            };
            return Task.FromResult(response);
        }
    }
}
