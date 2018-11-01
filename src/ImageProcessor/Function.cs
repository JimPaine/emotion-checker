using ImageProcessor.models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
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

            string image = await request.ReadAsStringAsync();
            
            try
            {
                log.Info("Building configuration");
                IConfigurationRoot config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
                log.Info("Completed building configuration");

                string vaultUri = config["vault_uri"];
                log.Info($"Vault Uri: {vaultUri}");

                log.Info("Getting Secrets from vault");
                Random random = new Random();
                int n = random.Next(0,3);
                string instance = n > 0 ? n.ToString() : string.Empty;
                string secret = await GetSecret(vaultUri, $"face-key{instance}", config, log);
                string uri = await GetSecret(vaultUri, $"face-endpoint{instance}", config, log);
                log.Info("Completed getting secrets");

                FaceResponse[] response = await GetEmotion(image, uri, secret, log);

                return response != null ? new OkObjectResult(response) : new NotFoundObjectResult("No faces found") as IActionResult;
            }
            catch (Exception exception)
            {
                Exception lowest = exception.GetBaseException() ?? exception;
                log.Error("Failed processing image", lowest);
                return new BadRequestObjectResult(lowest);
            }
        }

        private static async Task<string> GetSecret(string vaultUri, string secretName, IConfigurationRoot config, TraceWriter log)
        {
            using (HttpClient httpClient = HttpClientFactory.Create())
            {
                log.Info($"Looking up secret with name: {secretName} at {vaultUri}");
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {await GetVaultAccessToken(vaultUri, config, log)}");
                HttpResponseMessage response = await httpClient.GetAsync(new Uri($"{vaultUri}secrets/{secretName}?api-version=2016-10-01"));

                if (!response.IsSuccessStatusCode)
                {
                    log.Error($"Request to obtain secret {secretName}");
                    string reason = await response.Content.ReadAsStringAsync();
                    log.Error(reason);
                    throw new Exception($"Failed to get secret: {reason}");
                }
                else
                {
                    log.Info($"Successfully got secret: {secretName}");
                }

                dynamic result = await response.Content.ReadAsAsync<object>();
                string secret = result?.value;
                if (string.IsNullOrWhiteSpace(secret)) log.Error($"Failed to get value for secret : {secretName}");
                return secret;
            }            
        }

        private static async Task<string> GetVaultAccessToken(string resource, IConfigurationRoot config, TraceWriter log)
        {
            log.Info("Obtaining vault access token");

            using(HttpClient httpClient = HttpClientFactory.Create())
            {
                string secret = config["MSI_SECRET"];

                if (string.IsNullOrWhiteSpace(secret))
                {
                    Exception exception = new Exception("MSI_SECRET was not set");
                    log.Error("MSI_SECRET was not set", exception);
                    throw exception;
                }
                else
                {
                    log.Info("MSI Secret obtained");
                }

                httpClient.DefaultRequestHeaders.Add("Secret", secret);
                Uri uri = new Uri($"{config["MSI_ENDPOINT"]}?resource=https://vault.azure.net&api-version=2017-09-01");

                HttpResponseMessage response = await httpClient.GetAsync(uri);

                if (!response.IsSuccessStatusCode)
                {
                    log.Error("Request to obtain vault token was not successful");
                    string reason = await response.Content.ReadAsStringAsync();
                    log.Error(reason);
                    throw new Exception($"Failed to get vault token: {reason}");
                }
                else
                {
                    log.Info("Vault access token obtained");
                }

                try
                {
                    string result = await response.Content.ReadAsStringAsync();
                    log.Info(result);
                    dynamic item = JsonConvert.DeserializeObject(result);
                    string token = item.access_token;
                
                    if(string.IsNullOrWhiteSpace(token)) 
                    {
                        log.Error("token could not be read");
                        throw new Exception("token could not be read");
                    }
                    return token;
                } 
                catch(Exception exception)
                {
                    log.Error(exception.Message);
                    throw exception;
                }
                
            }
        }

        private static async Task<FaceResponse[]> GetEmotion(string image, string faceUri, string secret, TraceWriter log)
        {
            log.Info($"Attempt to check face emotion via {faceUri}");

            image = image.Replace("data:image/png;base64,", "");

            using (HttpClient httpClient = HttpClientFactory.Create())
            using (ByteArrayContent content = new ByteArrayContent(Convert.FromBase64String(image)))
            {
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", secret);

                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                HttpResponseMessage response = await httpClient.PostAsync(
                    new Uri($"{faceUri}/detect?returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,emotion,gender"), content);
                
                if (!response.IsSuccessStatusCode)
                {
                    log.Error("Request to emotion was not successful");
                    string reason = await response.Content.ReadAsStringAsync();
                    log.Error(reason);
                    throw new Exception($"Failed to get emotion: {reason} using uri {faceUri}");
                }
                
                log.Info("Emotion obtained");
                
                FaceResponse[] result = await response.Content.ReadAsAsync<FaceResponse[]>();     

                if(result == null || result.Length < 1) 
                {                    
                    return null;
                }   

                // for(int n = 0; n < result.Length; n++){
                   // GetEmotion(result[n].faceAttributes.emotion);
                // }
                return result;
            }     
        }

        private static void GetEmotion(FaceEmotion faceEmotion)
        {
            PropertyInfo[] properties = faceEmotion.GetType().GetProperties();
            double high = -1;
            string emotion = string.Empty;
            foreach (PropertyInfo property in properties)
            {
                if ((double)property.GetValue(faceEmotion) > high)
                {
                    high = (double)property.GetValue(faceEmotion);
                    emotion = property.Name;
                }
            }
            faceEmotion.result = emotion;
        }
    }
}