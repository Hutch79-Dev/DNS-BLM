using System.Reflection;

namespace DNS_BLM.Api.Services;

public static class AssamblySemVer
{
    /// <summary>
    /// Returns the AssemblyInformationalVersion without any "+" metadata, prefixed with "v".
    /// </summary>
    public static string GetSemanticVersion(this Assembly asm)
    {
        var info = asm
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?.Split('+', 2)[0];  

        return !string.IsNullOrWhiteSpace(info) 
            ? $"v{info}" 
            : "unknown";
    }
}