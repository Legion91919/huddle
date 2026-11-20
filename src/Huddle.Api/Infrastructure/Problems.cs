using Huddle.SharedKernel.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Huddle.Api.Infrastructure;

/// <summary>Every error is RFC 9457 ProblemDetails with a stable machine-readable "reason" and type urn:huddle:problem:&lt;reason&gt;.</summary>
internal static class Problems
{
    public static IResult NotAMember() =>
        Create(StatusCodes.Status403Forbidden, "not-a-member", "Not a member of this group", "Only members of this group can see or change it.");

    public static IResult GroupNotFound() =>
        Create(StatusCodes.Status404NotFound, "group-not-found", "Group not found", "That group does not exist.");

    public static IResult For(ContributionFailure failure, string detail) => failure switch
    {
        ContributionFailure.IdempotencyKeyMissing => Create(400, "idempotency-key-missing", "Idempotency-Key required", detail),
        ContributionFailure.IdempotencyKeyInvalid => Create(400, "idempotency-key-invalid", "Idempotency-Key not valid", detail),
        ContributionFailure.IdempotencyKeyReused => Create(409, "idempotency-key-reused", "Idempotency-Key already used", detail),
        ContributionFailure.GroupNotFound => GroupNotFound(),
        ContributionFailure.NotAMember => NotAMember(),
        ContributionFailure.AmountRequired => Invalid("amount-required", "Amount required", detail, "amount"),
        ContributionFailure.AmountNotPositive => Invalid("amount-not-positive", "Amount must be more than zero", detail, "amount"),
        ContributionFailure.AmountPrecision => Invalid("amount-precision", "Amount has fractions of a cent", detail, "amount"),
        ContributionFailure.CurrencyMismatch => Invalid("currency-mismatch", "Wrong currency", detail, "currency"),
        ContributionFailure.AboveLimit => Invalid("amount-above-limit", "Amount above your limit", detail, "amount"),
        ContributionFailure.InsufficientFunds => Invalid("insufficient-funds", "Not enough money in your wallet", detail, "amount"),
        _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, null),
    };

    private static ProblemHttpResult Invalid(string reason, string title, string detail, string field)
    {
        var problem = Build(StatusCodes.Status422UnprocessableEntity, reason, title, detail);
        problem.Extensions["errors"] = new Dictionary<string, string[]> { [field] = [detail] };
        return TypedResults.Problem(problem);
    }

    private static ProblemHttpResult Create(int status, string reason, string title, string detail) => TypedResults.Problem(Build(status, reason, title, detail));

    private static ProblemDetails Build(int status, string reason, string title, string detail)
    {
        var problem = new ProblemDetails { Status = status, Type = $"urn:huddle:problem:{reason}", Title = title, Detail = detail };
        problem.Extensions["reason"] = reason;
        return problem;
    }
}
