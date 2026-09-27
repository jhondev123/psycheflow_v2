using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Payments;

namespace Psycheflow.Api.UnitTests.Features.Payments;

public sealed class PaymentTests
{
    private static readonly DateOnly Today = new(2026, 10, 6);

    private static Payment NewPayment(decimal amount = 150m) => Payment.ForSession(Guid.CreateVersion7(), amount);

    [Fact]
    public void ForSession_StartsPending()
    {
        Payment payment = NewPayment();

        payment.Status.ShouldBe(PaymentStatus.Pending);
        payment.Amount.ShouldBe(150m);
        payment.Method.ShouldBeNull();
    }

    [Fact]
    public void ForSession_NegativeAmount_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => NewPayment(-1m));

    [Fact]
    public void Pay_CompletedSession_RegistersPayment()
    {
        Payment payment = NewPayment();

        Result result = payment.Pay(sessionCompleted: true, PaymentMethod.Pix, Today, 170m, " Pago no balcão ", Today);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Paid);
        payment.Method.ShouldBe(PaymentMethod.Pix);
        payment.PaidAt.ShouldBe(Today);
        payment.Amount.ShouldBe(170m);
        payment.Notes.ShouldBe("Pago no balcão");
    }

    [Fact]
    public void Pay_SessionNotCompleted_Fails() =>
        NewPayment().Pay(sessionCompleted: false, PaymentMethod.Pix, Today, null, null, Today)
            .Error.ShouldBe(PaymentErrors.SessionNotCompleted);

    [Fact]
    public void Pay_DateBeforeToday_Fails() =>
        NewPayment().Pay(sessionCompleted: true, PaymentMethod.Pix, Today.AddDays(-1), null, null, Today)
            .Error.ShouldBe(PaymentErrors.PaidAtInThePast);

    [Fact]
    public void Pay_AlreadyPaid_Fails()
    {
        Payment payment = NewPayment();
        payment.Pay(true, PaymentMethod.Pix, Today, null, null, Today);

        payment.Pay(true, PaymentMethod.DebitCard, Today, null, null, Today).Error.ShouldBe(PaymentErrors.NotPending);
    }

    [Fact]
    public void ChangeAmount_OnlyWhilePending()
    {
        Payment payment = NewPayment();

        payment.ChangeAmount(200m).IsSuccess.ShouldBeTrue();
        payment.Amount.ShouldBe(200m);

        payment.Pay(true, PaymentMethod.Pix, Today, null, null, Today);
        payment.ChangeAmount(10m).Error.ShouldBe(PaymentErrors.NotPending);
        payment.Amount.ShouldBe(200m);
    }

    [Fact]
    public void Cancel_PendingOrPaid_Works_ButNotTwice()
    {
        Payment pending = NewPayment();
        Payment paid = NewPayment();
        paid.Pay(true, PaymentMethod.Pix, Today, null, null, Today);

        pending.Cancel("Sessão cancelada").IsSuccess.ShouldBeTrue();
        paid.Cancel("Estorno").IsSuccess.ShouldBeTrue();

        paid.Status.ShouldBe(PaymentStatus.Cancelled);
        paid.CancellationReason.ShouldBe("Estorno");
        paid.Cancel("De novo").Error.ShouldBe(PaymentErrors.AlreadyCancelled);
    }
}
