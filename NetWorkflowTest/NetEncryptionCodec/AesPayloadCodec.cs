using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Google.Protobuf;
using System.Collections.Generic;
using Temporalio.Api.Common.V1;
using Temporalio.Converters;

namespace NetEncryptionCodec;


public class AesPayloadCodec : IPayloadCodec
{
    private const string DefaultKeyId = "enc-key-1";
    public static readonly byte[] DefaultKey = System.Text.Encoding.ASCII.GetBytes("test-key-test-key-test-key-test!");
    private readonly byte[] key;
    private static readonly ByteString EncodingByteString = ByteString.CopyFromUtf8("binary/encrypted");
    private readonly ByteString keyIDByteString;

    public string KeyID { get; private init; }

    public AesPayloadCodec(string keyID = DefaultKeyId, byte[]? key = null)
    {
        KeyID = keyID;
        keyIDByteString = ByteString.CopyFromUtf8(KeyID);
        this.key = key ?? DefaultKey;
    }

    public byte[] Encrypt(byte[] plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
        }
        return ms.ToArray();
    }

    public byte[] Decrypt(byte[] ciphertextWithIv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var iv = new byte[16];
        Array.Copy(ciphertextWithIv, 0, iv, 0, iv.Length);
        var cipher = new byte[ciphertextWithIv.Length - iv.Length];
        Array.Copy(ciphertextWithIv, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
        {
            cs.Write(cipher, 0, cipher.Length);
            cs.FlushFinalBlock();
        }
        return ms.ToArray();
    }

    public Task<IReadOnlyCollection<Payload>> EncodeAsync(IReadOnlyCollection<Payload> payloads) =>
    Task.FromResult<IReadOnlyCollection<Payload>>(payloads.Select(p =>
        {
            return new Payload()
            {
                Metadata =
                {
                    ["encoding"] = EncodingByteString,
                    ["encryption-key-id"] = keyIDByteString,
                },
                Data = ByteString.CopyFrom(Encrypt(p.ToByteArray())),
            };
        }).ToList());

    public Task<IReadOnlyCollection<Payload>> DecodeAsync(IReadOnlyCollection<Payload> payloads) =>
        Task.FromResult<IReadOnlyCollection<Payload>>(payloads.Select(p =>
        {
            // Ignore if it doesn't have our expected encoding
            if (p.Metadata.GetValueOrDefault("encoding") != EncodingByteString)
            {
                return p;
            }
            // Confirm same key
            var keyID = p.Metadata.GetValueOrDefault("encryption-key-id");
            if (keyID != keyIDByteString)
            {
                throw new InvalidOperationException($"Unrecognized key ID {keyID?.ToStringUtf8()}, expected {KeyID}");
            }
            // Decrypt
            return Payload.Parser.ParseFrom(Decrypt(p.Data.ToByteArray()));
        }).ToList());


    public static byte[] Encrypt(byte[] bytes, byte[] key)
    {
        var codec = new AesPayloadCodec(key: key);
        return codec.Encrypt(bytes);
    }

    public static byte[] Decrypt(byte[] ciphertextWithIv, byte[] key)
    {
        var codec = new AesPayloadCodec(key: key);
        return codec.Decrypt(ciphertextWithIv);
    }
}
