using API.Filters;
using API.Middlewares;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UnitTests.API;

public class ApiLayerTests
{
    [Fact]
    public void ExceptionFilter_ShouldReturnValidationResponse()
    {
        var logger = Substitute.For<ILogger<ExceptionFilter>>();
        var filter = new ExceptionFilter(logger);
        var validationException = new ValidationException([
            new FluentValidation.Results.ValidationFailure("ApiKey", "required")
        ]);
        var context = CreateExceptionContext(validationException);

        filter.OnException(context);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.ExceptionHandled.Should().BeTrue();
    }

    [Fact]
    public void ExceptionFilter_ShouldReturnUnauthorizedResponse()
    {
        var logger = Substitute.For<ILogger<ExceptionFilter>>();
        var filter = new ExceptionFilter(logger);
        var context = CreateExceptionContext(new UnauthorizedException("denied"));

        filter.OnException(context);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public void ExceptionFilter_ShouldReturnDomainResponse()
    {
        var logger = Substitute.For<ILogger<ExceptionFilter>>();
        var filter = new ExceptionFilter(logger);
        var context = CreateExceptionContext(new DomainException("domain"));

        filter.OnException(context);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void ExceptionFilter_ShouldLogUnhandledExceptions()
    {
        var logger = Substitute.For<ILogger<ExceptionFilter>>();
        var filter = new ExceptionFilter(logger);
        var context = CreateExceptionContext(new Exception("boom"));

        filter.OnException(context);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task CorrelationIdMiddleware_ShouldReuseIncomingHeader()
    {
        var middleware = new CorrelationIdMiddleware();
        var correlationService = Substitute.For<ICorrelationIdService>();
        var services = new ServiceCollection()
            .AddSingleton(correlationService)
            .BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        var correlationId = Guid.NewGuid();
        context.Request.Headers["X-Correlation-ID"] = correlationId.ToString();
        correlationService.GetCorrelationId().Returns(correlationId.ToString());

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        correlationService.Received(1).SetCorrelationId(correlationId);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_ShouldAppendGeneratedHeaderWhenMissing()
    {
        var middleware = new CorrelationIdMiddleware();
        var correlationService = Substitute.For<ICorrelationIdService>();
        var generatedId = Guid.NewGuid().ToString();
        correlationService.GetCorrelationId().Returns(generatedId);
        var services = new ServiceCollection()
            .AddSingleton(correlationService)
            .BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        correlationService.DidNotReceive().SetCorrelationId(Arg.Any<Guid>());
    }

    private static ExceptionContext CreateExceptionContext(Exception exception)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new(), new());
        return new ExceptionContext(actionContext, [])
        {
            Exception = exception
        };
    }
}
