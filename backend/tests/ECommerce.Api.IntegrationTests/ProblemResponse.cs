namespace ECommerce.Api.IntegrationTests;

public sealed record ProblemResponse(
    string? Type,
    string? Title,
    int Status,
    string? Detail,
    string? Instance,
    string? TraceId,
    string? OrderNumber,
    Dictionary<string, string[]>? Errors);
