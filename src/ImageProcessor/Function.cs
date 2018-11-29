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
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
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
        private static string faceKey = Environment.GetEnvironmentVariable("face-key");
        private static string faceEndpoint = Environment.GetEnvironmentVariable("face-endpoint");

        [FunctionName("EmotionChecker")]        
        public static async Task<IActionResult> Check(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest request, TraceWriter log)
        {
            log.Info("Processing request");
           
            try
            {                                
                string image = await request.ReadAsStringAsync();

                IList<DetectedFace> response = await GetEmotion(image, faceEndpoint, faceKey, log);

                return response != null ? new OkObjectResult(response) : new NotFoundObjectResult("No faces found") as IActionResult;
            }
            catch (Exception exception)
            {
                log.Error("Failed processing image", exception);
                return new BadRequestObjectResult(exception);
            }
        }

        private static async Task<IList<DetectedFace>> GetEmotion(string image, string faceUri, string secret, TraceWriter log)
        {
            log.Info($"Attempt to check face emotion via {faceUri}");

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
                    log.Error("Request to emotion was not successful");
                    string reason = await response.Content.ReadAsStringAsync();
                    log.Error(reason);
                    throw new Exception($"Failed to get emotion: {reason} using uri {faceUri}");
                }               

                log.Info("Emotion obtained");               

                IList<DetectedFace> result = await response.Content.ReadAsAsync<IList<DetectedFace>>();     

                if(result == null || !result.Any()) 
                {                    
                    return null;
                }

                return result;
            }     

        }
    }
}