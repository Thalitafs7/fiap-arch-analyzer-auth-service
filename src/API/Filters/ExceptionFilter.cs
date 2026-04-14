using Application.Common.Exceptions;
using Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Filters
{
    public class ExceptionFilter(ILogger<ExceptionFilter> logger) : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            var (statusCode, message) = exception switch
            {
                ValidationException validationEx => (
                    StatusCodes.Status400BadRequest,
                    new
                    {
                        message = "Erro de validação",
                        errors = validationEx.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage })
                    }
                ),
                UnauthorizedException unauthorizedException => (
                    StatusCodes.Status401Unauthorized,
                    new { message = unauthorizedException.Message }
                ),
                DomainException domainEx => (
                    StatusCodes.Status400BadRequest,
                    new { message = domainEx.Message }
                ),
                _ => (
                    StatusCodes.Status500InternalServerError,
                    new { message = "Erro interno do servidor" } as object
                )
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Erro não tratado: {Message}", exception.Message);
            }

            context.Result = new ObjectResult(message) { StatusCode = statusCode };

            context.ExceptionHandled = true;
        }
    }
}
