using System.Buffers.Binary;
using System.Security.Cryptography;

namespace TgJobAdAnalytics.Services.Vectors;

/// <summary>
/// Serialization helpers for MinHash signatures and integrity hashing.
/// </summary>
public static class SignatureSerializer
{
    public static byte[] ToBytes(ReadOnlySpan<uint> signature)
    {
        var bytes = new byte[signature.Length * sizeof(uint)];
        var span = bytes.AsSpan();
        for (int i = 0; i < signature.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(i * 4, 4), signature[i]);
        return bytes;
    }

    public static uint[] FromBytes(byte[] bytes)
    {
        var len = bytes.Length / sizeof(uint);
        var result = new uint[len];
        var span = bytes.AsSpan();
        for (int i = 0; i < len; i++)
            result[i] = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(i * 4, 4));
        return result;
    }

    public static string Sha256Hex(ReadOnlySpan<byte> data)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(data.ToArray());
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
