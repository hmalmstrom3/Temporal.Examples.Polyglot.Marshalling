NetEncryptionCodec

A small C# class library providing AES encryption/decryption helpers useful for payload encoding/decoding.

Usage:
- Reference the project from your worker or client projects.
- Create an `AesPayloadCodec` with a 16/24/32-byte key.

Example:

```csharp
var key = Convert.FromBase64String(Environment.GetEnvironmentVariable("PAYLOAD_AES_KEY_B64"));
var codec = new AesPayloadCodec(key);
var encrypted = codec.Encrypt(System.Text.Encoding.UTF8.GetBytes("hello"));
var decrypted = codec.Decrypt(encrypted);
```

Notes:
- This library is intentionally minimal. For production, manage keys securely (Key Vault, KMS, etc.).
- The project references Temporalio for compatibility but does not directly implement Temporal interfaces by default.
