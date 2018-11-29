using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using ImageProcessor.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Willezone.Azure.WebJobs.Extensions.DependencyInjection;

namespace ImageProcessor
{
    public static class Function
    {

        [FunctionName("EmotionChecker")]        
        public static async Task<IActionResult> Check(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest request, 
            [Inject]IKeyVaultApiClient keyVaultClient,
            [Inject]IConfigurationRoot configuration,
            [Inject]IFaceApiClient faceApiClient,
            ILogger logger)
        {
            logger.LogInformation("Processing request");
           
            try
            {                                
                string image = await request.ReadAsStringAsync();

                IList<DetectedFace> response = await faceApiClient.DetectFaces(image);

                return response != null ? new OkObjectResult(response) : new NotFoundObjectResult("No faces found") as IActionResult;
            }
            catch (Exception exception)
            {
                logger.LogError("Failed processing image", exception);
                return new BadRequestObjectResult(exception);
            }
        }
    }
}