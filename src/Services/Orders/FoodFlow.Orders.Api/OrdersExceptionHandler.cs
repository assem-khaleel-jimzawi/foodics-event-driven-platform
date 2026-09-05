using System.Diagnostics;
using FoodFlow.Orders.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FoodFlow.Orders.Api;

internal sealed class OrdersExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
        {
            return false;
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid order",
            Detail = domainException.Message,
            Extensions =
            {
                ["code"] = domainException.Code,
                ["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier
            }
        };

        if (domainException.Code is "order_already_cancelled" or "order_cannot_be_cancelled" or "order_cannot_be_confirmed")
        {
            problem.Status = StatusCodes.Status409Conflict;
            problem.Title = "Order conflict";
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
