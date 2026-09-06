using System.Net;
using ContasEmDia.Api.Responses;

namespace ContasEmDia.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception)
        {
            var response = ApiResponse<object>.Failure(
                [new ApiError(Field: null, Message: "Ocorreu um erro inesperado. Tente novamente mais tarde.")]);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
