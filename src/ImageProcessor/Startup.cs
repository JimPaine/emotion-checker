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
            builder.Services.AddTransient<LoggerFactory>();
            builder.Services.AddSingleton<ILogger<Function>, Logger<Function>>();
            builder.Services.AddSingleton<HttpClient>(new HttpClient());
        }
    }
}