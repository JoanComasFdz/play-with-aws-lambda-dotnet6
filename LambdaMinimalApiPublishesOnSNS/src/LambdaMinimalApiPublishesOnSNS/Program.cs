using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using LambdaMinimalApiPublishesOnSNS;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add AWS Lambda support. When application is run in Lambda Kestrel is swapped out as the web server with Amazon.Lambda.AspNetCoreServer. This
// package will act as the webserver translating request and responses between the Lambda event source and ASP.NET Core.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "Welcome to running ASP.NET Core Minimal API on AWS Lambda");

static async Task<IResult> AddUser(
    [FromBody] User user,
    [FromServices] ILogger<Program> logger
    //, [FromServices] ISnsPublisher publisher
    )
{
    logger.LogInformation("The user will be added: {user}", user);

    AWSSDKHandler.RegisterXRayForAllServices();

    var snsTopicArn = Environment.GetEnvironmentVariable("AWS_SNS_TOPICARN");
    var currentAwsRegion = Environment.GetEnvironmentVariable("AWS_REGION");
    var regionEndpoint = RegionEndpoint.GetBySystemName(currentAwsRegion);
    var snsPublisher = new SnsPublisher(new AmazonSimpleNotificationServiceClient(regionEndpoint), snsTopicArn);

    await snsPublisher.PublishAsync(new CreateUserCommand(user));
    
    return Results.Ok();
}
app.MapPost("/users", AddUser);

app.Run();

public record User(string Name, string Address);

public record CreateUserCommand(User User);
