namespace Psycheflow.Api.Features.Payments;

/// <summary>RN-54.</summary>
public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Cancelled = 2,
}

/// <summary>RN-52.</summary>
public enum PaymentMethod
{
    CreditCard = 0,
    DebitCard = 1,
    Pix = 2,
}
