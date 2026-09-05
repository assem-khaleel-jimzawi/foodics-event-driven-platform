using Microsoft.AspNetCore.Mvc;

namespace FoodFlow.ServiceDefaults;

public static class ProblemDetailsHttp
{
    public static IResult ValidationProblem(IReadOnlyDictionary<string, string[]> errors) =>
        Results.ValidationProblem(errors, title: "Validation failed", statusCode: StatusCodes.Status400BadRequest);

    public static IResult NotFound(string title, string detail, string key, object identifier)
    {
        return Results.Problem(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = title,
            Detail = detail,
            Extensions = { [key] = identifier }
        });
    }

    public static IResult Conflict(string title, string detail, string code) =>
        Results.Problem(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = title,
            Detail = detail,
            Extensions = { ["code"] = code }
        });
}
