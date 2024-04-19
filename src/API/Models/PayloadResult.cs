public record PayloadResult(
    string Name,
    DateTimeOffset CreatedAt,
    string Message,
    DateTimeOffset FinishedAt
);
