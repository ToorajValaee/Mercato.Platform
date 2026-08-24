namespace Mercato.Application.Validation;

public sealed class ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static ValidationResult Success() => new() { IsValid = true };
}
