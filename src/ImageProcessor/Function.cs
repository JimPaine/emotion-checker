using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ImageProcessor
{
    public class Function
    {
        private readonly HttpClient httpClient;
        private ILogger logger;        

        public Function(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        [FunctionName("EmotionChecker")]        
        public async Task<IActionResult> Check(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest request, ILogger logger)
        {
        
            this.logger = logger;
            this.logger.LogInformation("Processing request");
           
            try
            {                                
                string image = await request.ReadAsStringAsync();

                IList<DetectedFace> response = await this.GetEmotion(image);

                return response != null ? new OkObjectResult(response) : new NotFoundObjectResult("No faces found") as IActionResult;
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception, "Failed processing image");
                return new BadRequestObjectResult(exception);
            }
        }

        private async Task<IList<DetectedFace>> GetEmotion(string image)
        {
            string faceKey = Environment.GetEnvironmentVariable("face-key");
            string faceEndpoint = Environment.GetEnvironmentVariable("face-endpoint");

            this.logger.LogInformation($"Attempt to check face emotion via {faceEndpoint}");

            image = image.Replace("data:image/jpeg;base64,", "");
            
            using (ByteArrayContent content = new ByteArrayContent(Convert.FromBase64String(image)))
            {
                this.httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", faceKey);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                HttpResponseMessage response = await this.httpClient.PostAsync(
                    new Uri($"{faceEndpoint}/detect?returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,emotion,gender"), content);                

                if (!response.IsSuccessStatusCode)
                {
                    this.logger.LogError("Request to emotion was not successful");
                    string reason = await response.Content.ReadAsStringAsync();
                    this.logger.LogError($"status: {response.StatusCode} - Reason: {reason}");
                    this.httpClient.DefaultRequestHeaders.ToList().ForEach(x => this.logger.LogError($"Headers: Key: {x.Key} Value: {x.Value}"));
                    throw new Exception($"Failed to get emotion: {reason} using uri {faceEndpoint}");
                }                               

                IList<DetectedFace> result = await response.Content.ReadAsAsync<IList<DetectedFace>>();     

                if(result == null || !result.Any()) 
                {
                    this.logger.LogInformation("Face not found");
                    return null;
                }

                this.logger.LogInformation("Face found");

                return result;
            }     

        }
    }
}