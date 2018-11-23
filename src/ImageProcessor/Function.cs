using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ImageProcessor
{
    public static class Function
    {
        [FunctionName("EmotionChecker")]        
        public static async Task<IActionResult> Check(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest request, TraceWriter log, ExecutionContext context)
        {
            log.Info("Processing request");

            IConfigurationRoot config = GetConfiguration(context, log);
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));  
           
            try
            {                                
                string image = await request.ReadAsStringAsync();
                string vaultUri = config[$"vault_uri"];

                string endpoint = await GetSecret(keyVaultClient, vaultUri, "face-endpoint", log);
                string key = await GetSecret(keyVaultClient, vaultUri, "face-key", log);

                FaceClient faceClient = new FaceClient(new ApiKeyServiceClientCredentials(key));
                faceClient.Endpoint = endpoint;

                IList<DetectedFace> response = await GetEmotion(faceClient, image, log);

                return response != null ? new OkObjectResult(response) : new NotFoundObjectResult("No faces found") as IActionResult;
            }
            catch (Exception exception)
            {
                log.Error("Failed processing image", exception);
                return new BadRequestObjectResult(exception);
            }
        }

        private static async Task<string> GetSecret(KeyVaultClient client, string uri, string key, TraceWriter log)
        {
            SecretBundle bundle = await client.GetSecretAsync(uri, key);
            return bundle.Value;         
        }

        private static IConfigurationRoot GetConfiguration(ExecutionContext context, TraceWriter log)
        {
            log.Info("Building configuration");

            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            log.Info("Completed building configuration");

            return config;
        }

        private static async Task<IList<DetectedFace>> GetEmotion(FaceClient faceClient, string image, TraceWriter log)
        {
            FaceAttributeType[] faceAttributes = { FaceAttributeType.Age, FaceAttributeType.Gender, FaceAttributeType.Emotion };

            using(Stream stream = new MemoryStream(Convert.FromBase64String(image)))
            {
                stream.Position = 0;
                return await faceClient.Face.DetectWithStreamAsync(stream, true, false, faceAttributes);
            }            
        }
    }
}