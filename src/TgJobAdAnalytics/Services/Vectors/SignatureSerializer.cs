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


    /// <summary>
    /// Converts a byte array to an array of 32-bit unsigned integers using little-endian byte order.
    /// </summary>
    /// <remarks>If the length of <paramref name="bytes"/> is not a multiple of 4, the extra bytes at the end
    /// are ignored. Each group of 4 bytes is interpreted as a single 32-bit unsigned integer in little-endian
    /// format.</remarks>
    /// <param name="bytes">The byte array containing the data to convert. The length must be a multiple of 4.</param>
    /// <returns>An array of 32-bit unsigned integers representing the converted values from the input byte array.</returns>
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
