using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using FluentValidation;
using Domain.Exceptions;

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
                    } as object
                ),
                DomainException domainEx => (
                    StatusCodes.Status400BadRequest,
                    new { message = domainEx.Message } as object
                ),
                InvalidOperationException invalidOpEx => (
                    StatusCodes.Status400BadRequest,
                    new { message = invalidOpEx.Message } as object
                ),
                KeyNotFoundException notFoundEx => (
                    StatusCodes.Status404NotFound,
                    new { message = notFoundEx.Message } as object
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
