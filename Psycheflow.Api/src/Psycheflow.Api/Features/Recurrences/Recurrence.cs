using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Recurrences;

/// <summary>
/// Regra que gera sessões periódicas de um paciente com um psicólogo (RF010 / UC10). As sessões são geradas em janelas
/// de 3 meses (D-05); <see cref="GeneratedUntil"/> guarda até onde já foram criadas.
/// </summary>
public sealed class Recurrence : Entity, ITenantEntity, ISoftDeletable
{
    public const int ReasonMaxLength = 500;

    private Recurrence()
    {
    }

    public Guid CompanyId { get; private set; }

    public Guid PsychologistId { get; private set; }

    public Guid PatientId { get; private set; }

    public RecurrenceType Type { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly? EndDate { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public int DurationMinutes { get; private set; }

    /// <summary>Valor de cada sessão gerada (RN-51).</summary>
    public decimal Price { get; private set; }

    public DateOnly GeneratedUntil { get; private set; }

    public bool IsActive { get; private set; }

    public string? EndReason { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>Próxima janela a gerar começa no dia seguinte ao último gerado.</summary>
    public DateOnly NextWindowStart => GeneratedUntil < StartDate ? StartDate : GeneratedUntil.AddDays(1);

    /// <summary>Não há mais datas a gerar quando a data final já foi coberta.</summary>
    public bool IsFullyGenerated => EndDate is { } end && GeneratedUntil >= end;

    public static Recurrence Create(
        Guid psychologistId, Guid patientId, RecurrenceType type, DateOnly startDate, DateOnly? endDate,
        TimeOnly startTime, int durationMinutes, decimal price) =>
        new()
        {
            PsychologistId = psychologistId,
            PatientId = patientId,
            Type = type,
            StartDate = startDate,
            EndDate = endDate,
            StartTime = startTime,
            DurationMinutes = durationMinutes,
            Price = price,
            GeneratedUntil = startDate.AddDays(-1),
            IsActive = true,
        };

    public void MarkGeneratedUntil(DateOnly date) => GeneratedUntil = date;

    public Result End(string? reason)
    {
        if (!IsActive)
        {
            return RecurrenceErrors.NotActive;
        }

        IsActive = false;
        EndReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        return Result.Success();
    }
}

public static class RecurrenceErrors
{
    public static readonly Error NotFound = Error.NotFound("recurrence.not_found", "Recorrência não encontrada.");

    public static readonly Error NotActive = Error.Conflict("recurrence.not_active", "A recorrência já foi encerrada.");

    public static readonly Error FullyGenerated = Error.Conflict(
        "recurrence.fully_generated", "Todas as sessões até a data final da recorrência já foram geradas.");

    public static readonly Error StartInThePast = Error.Validation(
        "recurrence.start_in_past", "A data inicial não pode ser anterior a hoje.", "startDate");
}
