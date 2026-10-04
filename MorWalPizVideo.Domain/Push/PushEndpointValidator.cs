using System.Net;
using System.Net.Sockets;

namespace MorWalPizVideo.Domain.Push;

/// <summary>Validates browser push service endpoints before persistence and delivery.</summary>
public static class PushEndpointValidator
{
    public static async Task<bool> IsSafeAsync(string? value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.IsLoopback ||
            uri.HostNameType == UriHostNameType.Unknown)
            return false;

        if (IPAddress.TryParse(uri.Host, out var literal))
            return IsGloballyRoutableUnicast(literal);

        if (uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            return false;

        return (await GetSafeAddressesAsync(uri.Host, cancellationToken)).Length > 0;
    }

    /// <summary>
    /// Resolves the host and returns only public addresses. Callers that open a connection must use this exact
    /// result so a later DNS answer cannot redirect the connection to a private destination.
    /// </summary>
    public static async Task<IPAddress[]> GetSafeAddressesAsync(string host, CancellationToken cancellationToken = default)
    {
        if (IPAddress.TryParse(host, out var literal))
            return IsGloballyRoutableUnicast(literal) ? [literal] : [];
        if (host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            return [];

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        }
        catch (SocketException)
        {
            return [];
        }

        return addresses.Where(IsGloballyRoutableUnicast).Distinct().ToArray();
    }

    private static bool IsGloballyRoutableUnicast(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            return IsGloballyRoutableUnicast(address.MapToIPv4());

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            var value = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
            return !IsInIpv4Range(value, 0x00000000, 8) && // "this" network
                   !IsInIpv4Range(value, 0x0A000000, 8) && // private
                   !IsInIpv4Range(value, 0x64400000, 10) && // shared address space / CGNAT
                   !IsInIpv4Range(value, 0x7F000000, 8) && // loopback
                   !IsInIpv4Range(value, 0xA9FE0000, 16) && // link-local
                   !IsInIpv4Range(value, 0xAC100000, 12) && // private
                   !IsInIpv4Range(value, 0xC0000000, 24) && // IETF protocol assignments
                   !IsInIpv4Range(value, 0xC0000200, 24) && // documentation
                   !IsInIpv4Range(value, 0xC0586300, 24) && // 6to4 relay anycast
                   !IsInIpv4Range(value, 0xC0A80000, 16) && // private
                   !IsInIpv4Range(value, 0xC01FC400, 24) && // AS112 anycast
                   !IsInIpv4Range(value, 0xC034C100, 24) && // AMT anycast
                   !IsInIpv4Range(value, 0xC0AF3000, 24) && // AS112 anycast
                   !IsInIpv4Range(value, 0xC6120000, 15) && // benchmarking
                   !IsInIpv4Range(value, 0xC6336400, 24) && // documentation
                   !IsInIpv4Range(value, 0xCB007100, 24) && // documentation
                   !IsInIpv4Range(value, 0xE0000000, 4); // multicast and reserved
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
            return false;

        return !IPAddress.IsLoopback(address) &&
               !address.IsIPv6LinkLocal &&
               !IsInIpv6Range(address, "fc00::", 7) && // unique local
               !IsInIpv6Range(address, "fec0::", 10) && // site-local
               !IsInIpv6Range(address, "::", 8) && // unspecified, reserved and deprecated IPv4-compatible
               !IsInIpv6Range(address, "0064:ff9b::", 96) && // NAT64 well-known prefix
               !IsInIpv6Range(address, "0064:ff9b:0001::", 48) && // NAT64 local-use prefix
               !IsInIpv6Range(address, "100::", 64) && // discard-only
               !IsInIpv6Range(address, "2001:1::", 48) && // benchmarking
               !IsInIpv6Range(address, "2001:2::", 48) && // benchmarking
               !IsInIpv6Range(address, "2001:3::", 32) && // AMT
               !IsInIpv6Range(address, "2001:4:112::", 48) && // AS112 anycast
               !IsInIpv6Range(address, "2001:10::", 28) && // ORCHID
               !IsInIpv6Range(address, "2001:20::", 28) && // ORCHIDv2
               !IsInIpv6Range(address, "2001::", 32) && // Teredo
               !IsInIpv6Range(address, "2001:db8::", 32) && // documentation
               !IsInIpv6Range(address, "2002::", 16) && // 6to4
               !IsInIpv6Range(address, "3ffe::", 16) && // 6bone
               !IsInIpv6Range(address, "3fff::", 20) && // documentation
               !IsInIpv6Range(address, "ff00::", 8); // multicast
    }

    private static bool IsInIpv4Range(uint address, uint network, int prefixLength)
        => (address & (uint.MaxValue << (32 - prefixLength))) == network;

    private static bool IsInIpv6Range(IPAddress address, string network, int prefixLength)
    {
        var addressBytes = address.GetAddressBytes();
        var networkBytes = IPAddress.Parse(network).GetAddressBytes();
        var wholeBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        if (!addressBytes.AsSpan(0, wholeBytes).SequenceEqual(networkBytes.AsSpan(0, wholeBytes)))
            return false;

        return remainingBits == 0 ||
               (addressBytes[wholeBytes] & (byte)(0xFF << (8 - remainingBits))) ==
               (networkBytes[wholeBytes] & (byte)(0xFF << (8 - remainingBits)));
    }
}
