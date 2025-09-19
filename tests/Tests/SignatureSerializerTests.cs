using TgJobAdAnalytics.Services.Vectors;

namespace Tests;

public class SignatureSerializerTests
{
    [Fact]
    public void ToBytes_And_FromBytes_RoundTrip()
    {
        uint[] signature = [0u, 1u, 123456789u, uint.MaxValue - 10];
        var bytes = SignatureSerializer.ToBytes(signature);
        var restored = SignatureSerializer.FromBytes(bytes);

        Assert.Equal(signature, restored);
    }


    [Fact]
    public void Sha256Hex_ComputesDeterministicValue()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var hash1 = SignatureSerializer.Sha256Hex(data);
        var hash2 = SignatureSerializer.Sha256Hex(data);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // hex length
    }
}
