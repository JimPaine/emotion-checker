using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(ImageProcessor.Startup))]

namespace ImageProcessor;
public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        string faceKey = Environment.GetEnvironmentVariable("face-key") ?? throw new ArgumentNullException("face-key");
        string faceEndpoint = Environment.GetEnvironmentVariable("face-endpoint") ?? throw new ArgumentNullException("face-endpoint");

        HttpClient httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(faceEndpoint);
        httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", faceKey);

        builder.Services.AddSingleton<HttpClient>(httpClient);
        builder.Services.AddLogging();
    }
}