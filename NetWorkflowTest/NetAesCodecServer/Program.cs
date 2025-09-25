using Temporalio.Api.Common.V1;
using Temporalio.Converters;
using Google.Protobuf;
using NetEncryptionCodec;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSimpleConsole().SetMinimumLevel(LogLevel.Information);
builder.Services.AddSingleton<IPayloadCodec>(ctx => new AesPayloadCodec());
builder.Services.AddCors();
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

app.UseCors(builder =>
    builder.WithHeaders("content-type", "x-namespace").
    WithMethods("POST").
    WithOrigins("http://localhost:8080", "http://localhost:8233"));

app.MapPost("/encode", EncodeAsync);
/*async (HttpContext http) =>
{
    using var ms = new MemoryStream();
    await http.Request.Body.CopyToAsync(ms);
    var input = ms.ToArray();
    var outBytes = codec.Encrypt(input);
    http.Response.ContentType = "application/octet-stream";
    await http.Response.Body.WriteAsync(outBytes);
});*/

app.MapPost("/decode", DecodeAsync);
/*async (HttpContext http) =>
{
    using var ms = new MemoryStream();
    await http.Request.Body.CopyToAsync(ms);
    var input = ms.ToArray();
    var outBytes = codec.Decrypt(input);
    http.Response.ContentType = "application/octet-stream";
    await http.Response.Body.WriteAsync(outBytes);
} */

app.Run();

static Task<IResult> EncodeAsync(
        HttpContext ctx, IPayloadCodec codec) => ApplyCodecFuncAsync(ctx, codec.EncodeAsync);

static Task<IResult> DecodeAsync(
        HttpContext ctx, IPayloadCodec codec) => ApplyCodecFuncAsync(ctx, codec.DecodeAsync);

static async Task<IResult> ApplyCodecFuncAsync(
        HttpContext ctx, Func<IReadOnlyCollection<Payload>, Task<IReadOnlyCollection<Payload>>> func)
    {
        // Read payloads as JSON
        if (ctx.Request.ContentType?.StartsWith("application/json") != true)
        {
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }
        Payloads inPayloads;
        using (var reader = new StreamReader(ctx.Request.Body))
        {
            inPayloads = JsonParser.Default.Parse<Payloads>(await reader.ReadToEndAsync());
        }

        // Apply codec func
        var outPayloads = new Payloads() { Payloads_ = { await func(inPayloads.Payloads_) } };

        // Return JSON
        return Results.Text(JsonFormatter.Default.Format(outPayloads), "application/json");
    }