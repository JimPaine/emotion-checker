using System;
using System.Net.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(ImageProcessor.Startup))]

namespace ImageProcessor
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {           
            string faceKey = Environment.GetEnvironmentVariable("face-key");
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", faceKey);
            builder.Services.AddSingleton<HttpClient>(httpClient);
        }
    }
}