using FluentValidation;

namespace MannaHp.Server.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices
            .GetService<IValidator<T>>();

        if (validator is null)
            return await next(context);

        var arg = context.Arguments.OfType<T>().FirstOrDefault();
        if (arg is null)
            return await next(context);

        var result = await validator.ValidateAsync(arg);
        if (!result.IsValid)
            return Results.ValidationProblem(result.ToDictionary());

        return await next(context);
    }
}
