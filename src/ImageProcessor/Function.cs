using ImageProcessor.models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest request, ILogger log, ExecutionContext context)
        {
            log.Log(LogLevel.Information, "Processing request");

            string image = await request.ReadAsStringAsync();
            
            try
            {
                log.Log(LogLevel.Information, "Building configuration");
                IConfigurationRoot config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
                log.Log(LogLevel.Information, "Completed building configuration");                

                AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                string vaultUri = config[$"vault_uri"];

                string endpoint = await GetSecret(keyVaultClient, vaultUri, "face-endpoint");
                string key = await GetSecret(keyVaultClient, vaultUri, "face-key");

                FaceResponse[] response = await GetEmotion(image, endpoint, key, log);

                return response != null ? new OkObjectResult(response) : new NotFoundObjectResult("No faces found") as IActionResult;
            }
            catch (Exception exception)
            {
                log.Log(LogLevel.Error, "Failed processing image", exception);
                return new BadRequestObjectResult(exception);
            }
        }

        private static async Task<string> GetSecret(KeyVaultClient client, string uri, string key)
        {
            SecretBundle bundle = await client.GetSecretAsync(uri, key);
            return bundle.Value;
        }

        private static async Task<FaceResponse[]> GetEmotion(string image, string faceUri, string secret, ILogger log)
        {
            log.Log(LogLevel.Information, $"Attempt to check face emotion via {faceUri}");

            image = image.Replace("data:image/jpeg;base64,", "");

            using (HttpClient httpClient = HttpClientFactory.Create())
            using (ByteArrayContent content = new ByteArrayContent(Convert.FromBase64String(image)))
            {
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", secret);

                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                HttpResponseMessage response = await httpClient.PostAsync(
                    new Uri($"{faceUri}/detect?returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,emotion,gender"), content);
                
                if (!response.IsSuccessStatusCode)
                {
                    log.Log(LogLevel.Error, "Request to emotion was not successful");
                    string reason = await response.Content.ReadAsStringAsync();
                    log.Log(LogLevel.Error, reason);
                    throw new Exception($"Failed to get emotion: {reason} using uri {faceUri}");
                }
                
                log.Log(LogLevel.Information, "Emotion obtained");
                
                FaceResponse[] result = await response.Content.ReadAsAsync<FaceResponse[]>();     

                if(result == null || result.Length < 1) 
                {                    
                    return null;
                }
                return result;
            }     
        }
    }
}