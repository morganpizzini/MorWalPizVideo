using System.Net;
using MorWalPizVideo.Domain.Push;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class PushEndpointValidatorTests
{
    [Theory]
    [InlineData("100.64.0.1")]
    [InlineData("100.127.255.254")]
    [InlineData("192.0.2.1")]
    [InlineData("198.51.100.1")]
    [InlineData("203.0.113.1")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("::")]
    [InlineData("2001:db8::1")]
    [InlineData("ff02::1")]
    [InlineData("::ffff:192.168.1.1")]
    [InlineData("64:ff9b::c000:0201")]
    [InlineData("2001:0000:4136:e378:8000:63bf:3fff:fdd2")]
    public async Task Special_purpose_addresses_are_not_safe(string address)
    {
        var safeAddresses = await PushEndpointValidator.GetSafeAddressesAsync(address);

        Assert.Empty(safeAddresses);
    }

    [Theory]
    [InlineData("1.1.1.1")]
    [InlineData("8.8.8.8")]
    [InlineData("2001:4860:4860::8888")]
    public async Task Globally_routable_unicast_addresses_are_safe(string address)
    {
        var safeAddresses = await PushEndpointValidator.GetSafeAddressesAsync(address);

        Assert.Equal([IPAddress.Parse(address)], safeAddresses);
    }

    [Fact]
    public async Task Mapped_public_ipv4_is_classified_as_public_ipv4()
    {
        var safeAddresses = await PushEndpointValidator.GetSafeAddressesAsync("::ffff:8.8.8.8");

        Assert.Equal([IPAddress.Parse("::ffff:8.8.8.8")], safeAddresses);
    }
}
