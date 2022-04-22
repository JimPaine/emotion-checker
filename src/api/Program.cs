using System.Net.Http.Headers;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

string faceKey = Environment.GetEnvironmentVariable("face-key") ?? throw new ArgumentNullException("face-key");
string faceEndpoint = Environment.GetEnvironmentVariable("face-endpoint") ?? throw new ArgumentNullException("face-endpoint");

var client = new HttpClient { BaseAddress = new Uri(faceEndpoint) };
client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", faceKey);

var builder = WebApplication.CreateBuilder(args);

string policyName = "_allowlocal";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: policyName,
                      policy  =>
                      {
                          policy.WithOrigins("http://localhost:8087");
                          policy.AllowAnyHeader();
                          policy.AllowAnyMethod();
                      });
});

var app = builder.Build();

app.UseCors(policyName);

app.MapPost("api/face", async (HttpRequest request) => {
    string face = await new StreamReader(request.Body).ReadToEndAsync();
    using (ByteArrayContent content = new ByteArrayContent(Convert.FromBase64String(face.Replace("data:image/jpeg;base64,", ""))))
    {
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        HttpResponseMessage response = await client.PostAsync("face/v1.0/detect?returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,emotion", content);
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) return Results.BadRequest($"Failed to get emotion: {body} using uri {client.BaseAddress}");

        return result != null && result.Any() ? Results.Ok(body) : Results.BadRequest();
    }
});

app.Run();