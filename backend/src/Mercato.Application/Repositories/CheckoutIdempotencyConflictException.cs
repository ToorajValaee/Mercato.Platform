namespace Mercato.Application.Repositories;

public sealed class CheckoutIdempotencyConflictException : Exception
{
    public CheckoutIdempotencyConflictException(string idempotencyKey, Exception innerException)
        : base($"Checkout idempotency key '{idempotencyKey}' already exists.", innerException)
    {
        IdempotencyKey = idempotencyKey;
    }

    public string IdempotencyKey { get; }
}
