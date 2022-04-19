using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ImageProcessor;

public class Function
{
    private readonly HttpClient httpClient;
    private readonly ILogger<Function> logger;
    public Function(HttpClient httpClient, ILogger<Function> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    [FunctionName("EmotionChecker")]
    public async Task<IActionResult> Check(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest request)
    {
        this.logger.LogInformation("Processing request");
        try
        {
            string image = await request.ReadAsStringAsync();
            IList<DetectedFace>? response = await this.GetEmotion(image);
            return response != null && response.Any() ? new OkObjectResult(response) : new NotFoundObjectResult("No faces found") as IActionResult;
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Failed processing image");
            return new BadRequestObjectResult(exception);
        }
    }
    private async Task<IList<DetectedFace>?> GetEmotion(string image)
    {
        this.logger.LogInformation($"Attempt to check face emotion via {this.httpClient.BaseAddress}");
        image = image.Replace("data:image/jpeg;base64,", "");

        using (ByteArrayContent content = new ByteArrayContent(Convert.FromBase64String(image)))
        {
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            HttpResponseMessage response = await this.httpClient.PostAsync("face/v1.0/detect?returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,emotion,gender", content);
            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogError("Request to emotion was not successful");
                string reason = await response.Content.ReadAsStringAsync();
                this.logger.LogError($"status: {response.StatusCode} - Reason: {reason}");
                this.httpClient.DefaultRequestHeaders.ToList().ForEach(x => this.logger.LogError($"Headers: Key: {x.Key} Value: {x.Value}"));
                throw new Exception($"Failed to get emotion: {reason} using uri {this.httpClient.BaseAddress}");
            }
            IList<DetectedFace> result = await response.Content.ReadAsAsync<IList<DetectedFace>>();
            this.logger.LogInformation(result == null || !result.Any() ? "Face not found" : "Face found");
            return result;
        }
    }
}