using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Xunit;
using NSubstitute;
using NSubstitute.Core.Arguments;

namespace ImageProcessor.Tests
{
    public class HttpMocker : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> func;

        public HttpMocker(Func<HttpRequestMessage, Task<HttpResponseMessage>> func)
        {
            this.func = func;
        }
        
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return this.func(request);
        }
    }
    
    public class FunctionTests
    {
        [Fact]
        public async Task Function_ImageWithEncodingInContent_StrippedOut()
        {
            // Setup
            Environment.SetEnvironmentVariable("face-key", "face-key");
            Environment.SetEnvironmentVariable("face-endpoint", "http://localhost");
            string encoding = $"data:image/jpeg;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes("KEEPME"))}";
            HttpRequest request = Substitute.For<HttpRequest>();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(encoding));
            request.Body.Returns(stream);
            
            bool pass = false;
            Func<HttpRequestMessage, Task<HttpResponseMessage>> func = async r =>
            {
                string content = await r.Content.ReadAsStringAsync();
                pass = content.Equals("KEEPME");
                return new HttpResponseMessage();
            };
            
            HttpClient client = new HttpClient(new HttpMocker(func));
            ILogger logger = Substitute.For<ILogger>();
            
            // Act
            Function function = new Function(client, logger);
            await function.Check(request);
            
            
            // Assert
            Assert.True(pass);
        }

        [Fact]
        public async Task Function_Uri_Correct()
        {
            // Setup
            Environment.SetEnvironmentVariable("face-key", "face-key");
            Environment.SetEnvironmentVariable("face-endpoint", "http://localhost");
            string encoding = Convert.ToBase64String(Encoding.UTF8.GetBytes("KEEPME"));
            HttpRequest request = Substitute.For<HttpRequest>();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(encoding));
            request.Body.Returns(stream);
            
            bool pass = false;
            Func<HttpRequestMessage, Task<HttpResponseMessage>> func = async r =>
            {
                await Task.Run(() => { });
                pass = r.RequestUri.ToString().Equals("http://localhost/detect?returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,emotion,gender");
                return new HttpResponseMessage();
            };
            
            HttpClient client = new HttpClient(new HttpMocker(func));
            ILogger logger = Substitute.For<ILogger>();
            
            // Act
            Function function = new Function(client, logger);
            await function.Check(request);
            
            
            // Assert
            Assert.True(pass);
        }
        
        [Fact]
        public async Task Function_FaceApiNonSuccess_ThrowsException()
        {
            // Setup
            Environment.SetEnvironmentVariable("face-key", "face-key");
            Environment.SetEnvironmentVariable("face-endpoint", "http://localhost");
            string encoding = Convert.ToBase64String(Encoding.UTF8.GetBytes("KEEPME"));
            HttpRequest request = Substitute.For<HttpRequest>();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(encoding));
            request.Body.Returns(stream);
            string expected = "I went bang";
            
            Func<HttpRequestMessage, Task<HttpResponseMessage>> func = async r =>
            {
                await Task.Run(() => { });
                return new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = new StringContent(expected)
                };
            };
            
            HttpClient client = new HttpClient(new HttpMocker(func));
            ILogger logger = Substitute.For<ILogger>();
            
            // Act
            Function function = new Function(client, logger);
            IActionResult response = await function.Check(request);
            
            // Assert
            Assert.IsType<BadRequestObjectResult>(response);
            BadRequestObjectResult bad = response as BadRequestObjectResult;
            Assert.IsType<Exception>(bad.Value);
            Exception exception = bad.Value as Exception;
            Assert.Equal($"Failed to get emotion: {expected} using uri {Environment.GetEnvironmentVariable("face-endpoint")}", exception.Message);
        }

        [Fact]
        public async Task Function_FacesFound_200Response()
        {
            // Setup
            Environment.SetEnvironmentVariable("face-key", "face-key");
            Environment.SetEnvironmentVariable("face-endpoint", "http://localhost");
            string encoding = Convert.ToBase64String(Encoding.UTF8.GetBytes("KEEPME"));
            HttpRequest request = Substitute.For<HttpRequest>();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(encoding));
            request.Body.Returns(stream);
           
            
            Func<HttpRequestMessage, Task<HttpResponseMessage>> func = async r =>
            {
                await Task.Run(() => { });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ObjectContent(
                        typeof(List<DetectedFace>), 
                        new List<DetectedFace> { new DetectedFace { FaceId = Guid.NewGuid()}},
                        new JsonMediaTypeFormatter())
                };
            };
            
            HttpClient client = new HttpClient(new HttpMocker(func));
            ILogger logger = Substitute.For<ILogger>();
            
            // Act
            Function function = new Function(client, logger);
            IActionResult response = await function.Check(request);
            
            // Assert
            Assert.IsType<OkObjectResult>(response);
            
        }
        
        [Fact]
        public async Task Function_FacesNotFound_404Response()
        {
            // Setup
            Environment.SetEnvironmentVariable("face-key", "face-key");
            Environment.SetEnvironmentVariable("face-endpoint", "http://localhost");
            string encoding = Convert.ToBase64String(Encoding.UTF8.GetBytes("KEEPME"));
            HttpRequest request = Substitute.For<HttpRequest>();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(encoding));
            request.Body.Returns(stream);
           
            
            Func<HttpRequestMessage, Task<HttpResponseMessage>> func = async r =>
            {
                await Task.Run(() => { });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ObjectContent(
                        typeof(List<DetectedFace>), 
                        new List<DetectedFace>(),
                        new JsonMediaTypeFormatter())
                };
            };
            
            HttpClient client = new HttpClient(new HttpMocker(func));
            ILogger logger = Substitute.For<ILogger>();
            
            // Act
            Function function = new Function(client, logger);
            IActionResult response = await function.Check(request);
            
            // Assert
            Assert.IsType<NotFoundObjectResult>(response);
            
        }
        
        [Fact]
        public async Task Function_NullFromCogServices_404Response()
        {
            // Setup
            Environment.SetEnvironmentVariable("face-key", "face-key");
            Environment.SetEnvironmentVariable("face-endpoint", "http://localhost");
            string encoding = Convert.ToBase64String(Encoding.UTF8.GetBytes("KEEPME"));
            HttpRequest request = Substitute.For<HttpRequest>();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(encoding));
            request.Body.Returns(stream);
           
            
            Func<HttpRequestMessage, Task<HttpResponseMessage>> func = async r =>
            {
                await Task.Run(() => { });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ObjectContent(
                        typeof(List<DetectedFace>), 
                        null,
                        new JsonMediaTypeFormatter())
                };
            };
            
            HttpClient client = new HttpClient(new HttpMocker(func));
            ILogger logger = Substitute.For<ILogger>();
            
            // Act
            Function function = new Function(client, logger);
            IActionResult response = await function.Check(request);
            
            // Assert
            Assert.IsType<NotFoundObjectResult>(response);
            
        }
        
        [Fact]
        public async Task Function_ContentType_PassedToCogServices()
        {
            // Setup
            Environment.SetEnvironmentVariable("face-key", "face-key");
            Environment.SetEnvironmentVariable("face-endpoint", "http://localhost");
            string encoding = Convert.ToBase64String(Encoding.UTF8.GetBytes("KEEPME"));
            HttpRequest request = Substitute.For<HttpRequest>();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(encoding));
            request.Body.Returns(stream);

            bool pass = false;
            Func<HttpRequestMessage, Task<HttpResponseMessage>> func = async r =>
            {
                await Task.Run(() => { });
                pass = r.Content.Headers.ContentType.MediaType == "application/octet-stream";
                return new HttpResponseMessage(HttpStatusCode.OK);
            };
            
            HttpClient client = new HttpClient(new HttpMocker(func));
            ILogger logger = Substitute.For<ILogger>();
            
            // Act
            Function function = new Function(client, logger);
            IActionResult response = await function.Check(request);
            
            // Assert
            Assert.True(pass);
        }
        
        [Fact]
        public async Task Function_CogServicesKey_PassedInHeaders()
        {
            // Setup
            Environment.SetEnvironmentVariable("face-key", "face-key");
            Environment.SetEnvironmentVariable("face-endpoint", "http://localhost");
            string encoding = Convert.ToBase64String(Encoding.UTF8.GetBytes("KEEPME"));
            HttpRequest request = Substitute.For<HttpRequest>();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(encoding));
            request.Body.Returns(stream);

            bool pass = false;
            Func<HttpRequestMessage, Task<HttpResponseMessage>> func = async r =>
            {
                await Task.Run(() => { });
                pass = r.Headers.GetValues("Ocp-Apim-Subscription-Key").First() == "face-key";
                return new HttpResponseMessage(HttpStatusCode.OK);
            };
            
            HttpClient client = new HttpClient(new HttpMocker(func));
            ILogger logger = Substitute.For<ILogger>();
            
            // Act
            Function function = new Function(client, logger);
            IActionResult response = await function.Check(request);
            
            // Assert
            Assert.True(pass);
        }
    }
}
