using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MorWalPizVideo.Server.Services;

public static class YouTubeCredentialProvisioning
{
    public static string ResolvePath(IConfiguration configuration, IHostEnvironment? environment = null)
    {
        var path = configuration["YouTube:CredentialsPath"];
        if (string.IsNullOrWhiteSpace(path))
        {
            if (environment is not null && !environment.IsDevelopment())
                throw new InvalidOperationException("YouTube:CredentialsPath must reference externally provisioned credentials.");
            return "credentials.json";
        }

        if (!Path.IsPathFullyQualified(path))
            throw new InvalidOperationException("YouTube:CredentialsPath must be an absolute external path.");
        var fullPath = Path.GetFullPath(path);
        var relativePath = Path.GetRelativePath(AppContext.BaseDirectory, fullPath);
        if (!Path.IsPathRooted(relativePath) && relativePath != ".." && !relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("YouTube credentials cannot be provisioned inside the application output directory.");
        return fullPath;
    }
}