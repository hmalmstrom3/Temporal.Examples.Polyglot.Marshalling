using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using NetEncryptionCodec;
using Google.Protobuf;
using Temporalio.Api.Common.V1;

namespace NetEncryptionCodec.Tests;

public class AesPayloadCodecTests
{
    [Fact]
    public void EncryptDecrypt_RoundTrip_Works()
    {
        var key = Encoding.ASCII.GetBytes("0123456789ABCDEF0123456789ABCDEF"); // 32 bytes
        var codec = new AesPayloadCodec(key: key);
        var plain = Encoding.UTF8.GetBytes("hello world");

        var encrypted = codec.Encrypt(plain);
        Assert.NotNull(encrypted);
        Assert.NotEqual(plain, encrypted);

        var decrypted = codec.Decrypt(encrypted);
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void StaticHelpers_RoundTrip_Works()
    {
        var key = Encoding.ASCII.GetBytes("0123456789ABCDEF0123456789ABCDEF");
        var plain = Encoding.UTF8.GetBytes("static helper test");
        var encrypted = AesPayloadCodec.Encrypt(plain, key);
        var decrypted = AesPayloadCodec.Decrypt(encrypted, key);
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public async Task EncodeDecodeAsync_Payloads_RoundTrip()
    {
        var key = Encoding.ASCII.GetBytes("0123456789ABCDEF0123456789ABCDEF");
        var codec = new AesPayloadCodec(key: key);

        var payload = new Payload { Data = ByteString.CopyFromUtf8("somedata") };
        var encoded = await codec.EncodeAsync(new[] { payload });
        Assert.Single(encoded);
        var first = encoded.First();
        Assert.True(first.Metadata.ContainsKey("encoding"));
        Assert.True(first.Metadata.ContainsKey("encryption-key-id"));

        var decoded = await codec.DecodeAsync(encoded);
        Assert.Single(decoded);
        var decodedFirst = decoded.First();
        Assert.Equal(payload.Data.ToStringUtf8(), decodedFirst.Data.ToStringUtf8());
    }

    [Fact]
    public async Task DecodeAsync_WrongKey_Throws()
    {
        var codec1 = new AesPayloadCodec(keyID: "key1", key: Encoding.ASCII.GetBytes("0123456789ABCDEF0123456789ABCDEF"));
        var codec2 = new AesPayloadCodec(keyID: "key2", key: Encoding.ASCII.GetBytes("FEDCBA9876543210FEDCBA9876543210"));

        var payload = new Payload { Data = ByteString.CopyFromUtf8("somedata") };
        var encoded = await codec1.EncodeAsync(new[] { payload });

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await codec2.DecodeAsync(encoded));
    }
}
