using System.Net;
using System.Net.Http.Json;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Endpoints;
using Psycheflow.Api.Features.Payments;
using Psycheflow.Api.Features.Sessions;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Payments;

public sealed class PaymentsTests(ApiFactory factory) : SchedulingTest(factory)
{
    private Task<HttpResponseMessage> PayAsync(TestAccount account, Guid paymentId, object body) =>
        CreateClient(account).PostAsJsonAsync($"/api/v1/payments/{paymentId}/pay", body, Ct);

    private async Task<PaymentResponse> GetPaymentAsync(TestAccount account, Guid paymentId) =>
        (await CreateClient(account).GetFromJsonAsync<PaymentResponse>($"/api/v1/payments/{paymentId}", Json, Ct))!;

    [Fact]
    public async Task CreateSession_CreatesPendingPaymentWithCompanyDefaultPrice()
    {
        TestAccount account = await CreatePracticeAsync();
        await SetDefaultPriceAsync(account, 180m);
        Guid patientId = await CreatePatientAsync(account);

        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00");

        session.Payment.ShouldNotBeNull();
        session.Payment.Amount.ShouldBe(180m);
        session.Payment.Status.ShouldBe(PaymentStatus.Pending);
    }

    [Fact]
    public async Task CreateSession_WithExplicitPrice_OverridesDefault_AndWithoutAnyPriceStartsAtZero()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);

        SessionResponse withoutPrice = await CreateSessionAsync(account, patientId, Tomorrow, "08:00");
        SessionResponse withPrice = await CreateSessionAsync(account, patientId, Tomorrow, "09:00", price: 220.5m);

        withoutPrice.Payment!.Amount.ShouldBe(0m);
        withPrice.Payment!.Amount.ShouldBe(220.5m);
    }

    [Fact]
    public async Task Pay_BeforeSessionIsCompleted_Returns409()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00", price: 150m);

        HttpResponseMessage response = await PayAsync(account, session.Payment!.Id, new { method = "Pix" });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadProblemAsync(response)).Extensions["code"]!.ToString().ShouldBe("payment.session_not_completed");
    }

    [Fact]
    public async Task Pay_AfterCompletion_RegistersMethodDateAndAmount()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00", price: 150m);
        account = await CompleteSessionAsync(account, session);

        HttpResponseMessage response = await PayAsync(account, session.Payment!.Id, new
        {
            method = "CreditCard",
            amount = 160m,
            notes = "Parcelado em 2x",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        PaymentResponse paid = await ReadAsync<PaymentResponse>(response);
        paid.Status.ShouldBe(PaymentStatus.Paid);
        paid.Method.ShouldBe(PaymentMethod.CreditCard);
        paid.Amount.ShouldBe(160m);
        paid.PaidAt.ShouldBe(new DateOnly(2026, 10, 6));
        paid.Notes.ShouldBe("Parcelado em 2x");
    }

    [Theory]
    [InlineData("Pix", "2026-10-05", "paidAt")]
    [InlineData(null, null, "method")]
    public async Task Pay_InvalidData_Returns422(string? method, string? paidAt, string field)
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00", price: 150m);
        account = await CompleteSessionAsync(account, session);

        HttpResponseMessage response = await PayAsync(account, session.Payment!.Id, new { method, paidAt });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Errors.ShouldContainKey(field);
    }

    [Fact]
    public async Task UpdateAmount_WhilePending_Works_ButPaidPaymentIsLocked()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00", price: 150m);
        Guid paymentId = session.Payment!.Id;

        HttpResponseMessage pending = await CreateClient(account).PutAsJsonAsync($"/api/v1/payments/{paymentId}", new { amount = 175m }, Ct);
        account = await CompleteSessionAsync(account, session);
        (await PayAsync(account, paymentId, new { method = "Pix" })).EnsureSuccessStatusCode();
        HttpResponseMessage paid = await CreateClient(account).PutAsJsonAsync($"/api/v1/payments/{paymentId}", new { amount = 10m }, Ct);

        pending.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadAsync<PaymentResponse>(pending)).Amount.ShouldBe(175m);
        paid.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await GetPaymentAsync(account, paymentId)).Amount.ShouldBe(175m);
    }

    [Fact]
    public async Task CancelSession_CancelsItsPendingPayment()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00", price: 150m);

        (await PostActionAsync(account, session.Id, "cancel", new { reason = "Paciente desmarcou" })).EnsureSuccessStatusCode();

        PaymentResponse payment = await GetPaymentAsync(account, session.Payment!.Id);
        payment.Status.ShouldBe(PaymentStatus.Cancelled);
        payment.CancellationReason.ShouldBe("Sessão cancelada: Paciente desmarcou");
    }

    [Fact]
    public async Task CancelPayment_OfPaidPayment_RegistersRefund_AndCannotBeCancelledTwice()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00", price: 150m);
        account = await CompleteSessionAsync(account, session);
        (await PayAsync(account, session.Payment!.Id, new { method = "Pix" })).EnsureSuccessStatusCode();
        HttpClient client = CreateClient(account);

        HttpResponseMessage withoutReason = await client.PostAsJsonAsync($"/api/v1/payments/{session.Payment.Id}/cancel", new { reason = "" }, Ct);
        HttpResponseMessage refunded = await client.PostAsJsonAsync($"/api/v1/payments/{session.Payment.Id}/cancel", new { reason = "Estorno" }, Ct);
        HttpResponseMessage again = await client.PostAsJsonAsync($"/api/v1/payments/{session.Payment.Id}/cancel", new { reason = "De novo" }, Ct);

        withoutReason.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        refunded.StatusCode.ShouldBe(HttpStatusCode.OK);
        PaymentResponse payment = await ReadAsync<PaymentResponse>(refunded);
        payment.Status.ShouldBe(PaymentStatus.Cancelled);
        payment.CancellationReason.ShouldBe("Estorno");
        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteSession_RemovesItsPendingPayment()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00", price: 150m);
        HttpClient client = CreateClient(account);

        HttpResponseMessage deleted = await client.DeleteAsync($"/api/v1/sessions/{session.Id}", Ct);

        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent, await deleted.Content.ReadAsStringAsync(Ct));
        (await client.GetAsync($"/api/v1/payments/{session.Payment!.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_FiltersByStatusPatientAndPeriod_AndPsychologistSeesOnlyOwn()
    {
        TestAccount owner = await CreatePracticeAsync();
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);
        await SetWorkingHoursAsync(colleague, colleague.PsychologistId!.Value);
        Guid patientA = await CreatePatientAsync(owner);
        Guid patientB = await CreatePatientAsync(owner);
        SessionResponse a1 = await CreateSessionAsync(owner, patientA, Tomorrow, "08:00", price: 100m);
        SessionResponse b1 = await CreateSessionAsync(owner, patientB, Tomorrow, "09:00", price: 200m);
        await CreateSessionAsync(owner, patientA, "2026-10-20", "08:00", price: 100m);
        await CreateSessionAsync(colleague, patientA, Tomorrow, "08:00", price: 300m);
        (await PostActionAsync(owner, b1.Id, "cancel", new { reason = "x" })).EnsureSuccessStatusCode();
        HttpClient client = CreateClient(owner);

        PagedResponse<PaymentResponse> pending = (await client.GetFromJsonAsync<PagedResponse<PaymentResponse>>(
            $"/api/v1/payments?status=Pending&from={Tomorrow}&to={Tomorrow}", Json, Ct))!;
        PagedResponse<PaymentResponse> byPatient = (await client.GetFromJsonAsync<PagedResponse<PaymentResponse>>(
            $"/api/v1/payments?patientId={patientA}&psychologistId={owner.PsychologistId}", Json, Ct))!;
        PagedResponse<PaymentResponse> asColleague = (await CreateClient(colleague).GetFromJsonAsync<PagedResponse<PaymentResponse>>(
            "/api/v1/payments", Json, Ct))!;

        pending.Items.Select(p => p.Amount).ShouldBe([100m, 300m], ignoreOrder: true);
        byPatient.Items.Count.ShouldBe(2);
        byPatient.Items.ShouldContain(p => p.SessionId == a1.Id);
        asColleague.Items.Single().Amount.ShouldBe(300m);
    }

    [Fact]
    public async Task Get_PaymentOfColleague_Returns403_AndOfAnotherCompany_Returns404()
    {
        TestAccount owner = await CreatePracticeAsync();
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);
        TestAccount otherCompany = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(owner);
        SessionResponse session = await CreateSessionAsync(owner, patientId, Tomorrow, "14:00");

        HttpResponseMessage asColleague = await CreateClient(colleague).GetAsync($"/api/v1/payments/{session.Payment!.Id}", Ct);
        HttpResponseMessage asOtherCompany = await CreateClient(otherCompany).GetAsync($"/api/v1/payments/{session.Payment.Id}", Ct);

        asColleague.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        asOtherCompany.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
