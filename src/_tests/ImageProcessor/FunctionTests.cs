using System.Net;
using System.Net.Http.Formatting;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Extensions.Logging;
using Xunit;
using NSubstitute;

namespace ImageProcessor.Tests;

public class HttpMocker : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> func;
    public HttpMocker(Func<HttpRequestMessage, Task<HttpResponseMessage>> func) => this.func = func;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => this.func(request);
}
public class FunctionTests
{
    [Fact]
    public async Task Function_ImageWithEncodingInContent_StrippedOut()
    {
        // Setup
        string encoding = $"data:image/jpeg;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes("KEEPME"))}";
        HttpRequest request = Substitute.For<HttpRequest>();
        Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(encoding));
        request.Body.Returns(stream);
        bool pass = false;
        Func<HttpRequestMessage, Task<HttpResponseMessage>> func = async r =>
        {
            string content = await r!.Content!.ReadAsStringAsync();
            pass = content.Equals("KEEPME");
            return new HttpResponseMessage();
        };
        HttpClient client = new HttpClient(new HttpMocker(func)) { BaseAddress = new Uri("http://localhost") };
        ILogger<Function> logger = Substitute.For<ILogger<Function>>();
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
        string encoding = Convert.ToBase64String(Encoding.UTF8.GetBytes("KEEPME"));
        HttpRequest request = Substitute.For<HttpRequest>();
        Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(encoding));
        request.Body.Returns(stream);

        Uri? requestedUri = null;
        Func<HttpRequestMessage, Task<HttpResponseMessage>> func = r =>
        {
            requestedUri = r!.RequestUri;
            return Task.FromResult(new HttpResponseMessage());
        };

        HttpClient client = new HttpClient(new HttpMocker(func)) { BaseAddress = new Uri("http://localhost") };
        ILogger<Function> logger = Substitute.For<ILogger<Function>>();
        // Act
        Function function = new Function(client, logger);
        await function.Check(request);
        // Assert
        Assert.Equal("http://localhost/face/v1.0/detect?returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,emotion,gender", requestedUri!.ToString());
    }
    [Fact]
    public async Task Function_FaceApiNonSuccess_ThrowsException()
    {
        // Setup
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
        HttpClient client = new HttpClient(new HttpMocker(func)) { BaseAddress = new Uri("http://localhost") };
        ILogger<Function> logger = Substitute.For<ILogger<Function>>();
        // Act
        Function function = new Function(client, logger);
        IActionResult response = await function.Check(request);
        // Assert
        BadRequestObjectResult bad = Assert.IsType<BadRequestObjectResult>(response);
        Exception exception = Assert.IsType<Exception>(bad!.Value);
        Assert.Equal($"Failed to get emotion: {expected} using uri {client.BaseAddress}", exception.Message);
    }
    [Fact]
    public async Task Function_FacesFound_200Response()
    {
        // Setup
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
        HttpClient client = new HttpClient(new HttpMocker(func)) { BaseAddress = new Uri("http://localhost") };
        ILogger<Function> logger = Substitute.For<ILogger<Function>>();
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
        HttpClient client = new HttpClient(new HttpMocker(func)) { BaseAddress = new Uri("http://localhost") };
        ILogger<Function> logger = Substitute.For<ILogger<Function>>();
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
        HttpClient client = new HttpClient(new HttpMocker(func)) { BaseAddress = new Uri("http://localhost") };
        ILogger<Function> logger = Substitute.For<ILogger<Function>>();
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
        string encoding = Convert.ToBase64String(Encoding.UTF8.GetBytes("KEEPME"));
        HttpRequest request = Substitute.For<HttpRequest>();
        Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(encoding));
        request.Body.Returns(stream);
        bool pass = false;
        Func<HttpRequestMessage, Task<HttpResponseMessage>> func = async r =>
        {
            await Task.Run(() => { });
            pass = r!.Content!.Headers!.ContentType!.MediaType == "application/octet-stream";
            return new HttpResponseMessage(HttpStatusCode.OK);
        };
        HttpClient client = new HttpClient(new HttpMocker(func)) { BaseAddress = new Uri("http://localhost") };
        ILogger<Function> logger = Substitute.For<ILogger<Function>>();
        // Act
        Function function = new Function(client, logger);
        IActionResult response = await function.Check(request);
        // Assert
        Assert.True(pass);
    }
}