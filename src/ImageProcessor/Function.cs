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
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ImageProcessor
{
    public static class Function
    {
        [FunctionName("GetSignalRInfo")]
        public static SignalRConnectionInfo GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous)]HttpRequest req, 
            [SignalRConnectionInfo(HubName = "face")]SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }

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
                
                Random random = new Random();
                int instance = random.Next(0,3);
                log.Info($"Face API instance: {instance}");

                FaceResponse[] response = await GetEmotion(image, config[$"face-endpoint{instance}"], config[$"face-key{instance}"], log);

                return response != null ? new OkObjectResult(response) : new NotFoundObjectResult("No faces found") as IActionResult;
            }
            catch (Exception exception)
            {
                Exception lowest = exception.GetBaseException() ?? exception;
                log.Error("Failed processing image", lowest);
                return new BadRequestObjectResult(lowest);
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
                foreach(FaceResponse face in reulst)
                {
                    GetEmotion(face);
                }
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