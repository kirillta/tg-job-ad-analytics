using System.Security.Cryptography;
using System.Text;

namespace TgJobAdAnalytics.Utils;

/// <summary>
/// Helper for generating deterministic GUIDs from a namespace and name.
/// Implements RFC 4122-like v5 semantics using SHA-1.
/// </summary>
public static class DeterministicGuid
{
    /// <summary>
    /// Create a name-based GUID using SHA-1 over namespace and name.
    /// </summary>
    public static Guid Create(Guid @namespace, string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var nsBytes = @namespace.ToByteArray();
        var nameBytes = Encoding.UTF8.GetBytes(name);

        Span<byte> hash = stackalloc byte[20];
        using (var sha1 = SHA1.Create())
        {
            sha1.TransformBlock(nsBytes, 0, nsBytes.Length, null, 0);
            sha1.TransformFinalBlock(nameBytes, 0, nameBytes.Length);
            sha1.Hash.CopyTo(hash);
        }

        Span<byte> newGuid = stackalloc byte[16];
        hash[..16].CopyTo(newGuid);

        // Set version to 5 (name-based, SHA-1)
        newGuid[7] = (byte)((newGuid[7] & 0x0F) | (5 << 4));
        // Set variant to RFC 4122
        newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80);

        return new Guid(newGuid);
    }
}
