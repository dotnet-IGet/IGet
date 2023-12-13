using TestHelpers.Mocks;

namespace Tests;

public class MoreComplexHandlerTest
{
    [Fact]
    public void Test()
    {
        // Arrange
        var logger = new LoggerForComplexExample();
        var services = new TestServices();
        services.AddSingleton<ILogger>(logger);
        services.AddSingleton<ILoggerFactory>(new MockLoggerFactory(logger));
        services.AddSingleton(typeof(ILogger<MoreComplexHandler>), logger);
        services.AddIGet();
        var i = services.GetService<IGet>()!;

        // Act
        var request = new RequestX { Id = 1 };
        var result = i.Get<MoreComplexHandler>().Handle(request);

        // Assert
        Assert.Equal("[Information] Request 1 is valid.", logger.Logs[0]);
        Assert.Equal("[Information] Preprocessing request 1 complete.", logger.Logs[1]);
        Assert.Equal("[Information] Main processing of request 1 complete.", logger.Logs[2]);
        Assert.Equal("[Information] Start post processing request 1.", logger.Logs[3]);

        Assert.StartsWith(@"[Warning] Post processor failed for request 1.
The method or operation is not implemented.
   at Tests.RequestXPostProcessor.DoLessImportantStuffWith(RequestX request, WhatWasAskedFor whatWasAskedFor) in", logger.Logs[4]);

        Assert.NotNull(result);
    }
}

public class MoreComplexHandler(IGet i, ILogger<MoreComplexHandler> logger)
{
    public Result<WhatWasAskedFor> Handle(RequestX request)
    {
        try
        {
            var validationResult = i.Get<RequestXValidator>().Validate(request);
            if (validationResult.IsFail)
            {
                return Result.Fail<WhatWasAskedFor>(validationResult.ErrorMessages);
            }

            i.Get<RequestXPreProcessor>().Prepare(request);
            var whatWasAskedFor = i.Get<RequestXMainProcessor>().Handle(request);

            try
            {
                i.Get<RequestXPostProcessor>().DoLessImportantStuffWith(request, whatWasAskedFor);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Post processor failed for request {requestId}.", request.Id);
            }

            return Result.Success(whatWasAskedFor);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error for request {requestId}.", request.Id);
            return Result.Fail<WhatWasAskedFor>("Something went wrong. Try again later.");
        }
    }
}

public class RequestX { public int Id { get; set; } }

public class WhatWasAskedFor { }

public class RequestXValidator(ILogger logger)
{
    public Result Validate(RequestX request)
    {
        logger.LogInformation("Request {id} is valid.", request.Id);
        return Result.Success();
    }
}

public class RequestXPreProcessor(ILogger logger)
{
    internal void Prepare(RequestX request)
    {
        logger.LogInformation("Preprocessing request {requestId} complete.", request.Id);
    }
}

public class RequestXMainProcessor(ILogger logger)
{
    internal WhatWasAskedFor Handle(RequestX request)
    {
        logger.LogInformation("Main processing of request {requestId} complete.", request.Id);
        return new WhatWasAskedFor();
    }
}

public class RequestXPostProcessor(ILogger logger)
{
    internal void DoLessImportantStuffWith(RequestX request, WhatWasAskedFor whatWasAskedFor)
    {
        logger.LogInformation("Start post processing request {requestId}.", request.Id);
        throw new NotImplementedException();
    }
}

public class LoggerForComplexExample : TestLogger, ILogger<MoreComplexHandler>
{ }