using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using NetEncryptionCodec;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Key from env or default (dev only)
var keyB64 = Environment.GetEnvironmentVariable("PAYLOAD_AES_KEY_B64");
byte[] key;
if (!string.IsNullOrEmpty(keyB64))
{
    key = Convert.FromBase64String(keyB64);
}
else
{
    key = NetEncryptionCodec.AesPayloadCodec.DefaultKey;
}

var codec = new AesPayloadCodec(key: key);

app.MapPost("/encode", async (HttpContext http) =>
{
    using var ms = new MemoryStream();
    await http.Request.Body.CopyToAsync(ms);
    var input = ms.ToArray();
    var outBytes = codec.Encrypt(input);
    http.Response.ContentType = "application/octet-stream";
    await http.Response.Body.WriteAsync(outBytes);
});

app.MapPost("/decode", async (HttpContext http) =>
{
    using var ms = new MemoryStream();
    await http.Request.Body.CopyToAsync(ms);
    var input = ms.ToArray();
    var outBytes = codec.Decrypt(input);
    http.Response.ContentType = "application/octet-stream";
    await http.Response.Body.WriteAsync(outBytes);
});

app.Run();
