using Microsoft.EntityFrameworkCore.Storage;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Common.Time;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.Features.Scheduling.CreateBlock;

/// <summary>
/// UC19 / RF018: bloqueia dias inteiros ou uma faixa de horário (RN-39), ignorando o expediente (RN-40)
/// e sem conflitar com agendamentos (RN-38). Tudo ou nada: se um dia conflita, nenhum bloqueio é criado.
/// </summary>
public sealed class CreateBlockHandler(
    AppDbContext db, ScheduleAvailability availability, ICurrentUser currentUser, ClinicClock clock)
{
    public async Task<Result<IReadOnlyList<BlockResponse>>> Handle(CreateBlockRequest request, CancellationToken cancellationToken)
    {
        Result<Psychologist> psychologist = await availability.ResolvePsychologistAsync(request.PsychologistId, cancellationToken);
        if (psychologist.IsFailure)
        {
            return psychologist.Error;
        }

        CompanySettings settings = await db.GetCurrentSettingsAsync(currentUser, cancellationToken);
        DateTime localNow = clock.LocalNow(settings.TimeZone);

        List<TimeSlot> slots = [];
        for (DateOnly date = request.StartDate!.Value; date <= request.LastDate; date = date.AddDays(1))
        {
            Result<TimeSlot> slot = request.IsWholeDay
                ? TimeSlot.WholeDay(date)
                : TimeSlot.Create(date, request.StartTime!.Value, request.EndTime!.Value);
            if (slot.IsFailure)
            {
                return slot.Error;
            }

            slots.Add(slot.Value);
        }

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await availability.LockAgendaAsync(psychologist.Value.Id, cancellationToken);

        foreach (TimeSlot slot in slots)
        {
            Result available = await availability.CheckBlockSlotAsync(psychologist.Value.Id, slot, localNow, cancellationToken);
            if (available.IsFailure)
            {
                return available.Error;
            }
        }

        List<Schedule> blocks = [.. slots.Select(slot => Schedule.Block(psychologist.Value.Id, slot, request.Reason))];
        db.Schedules.AddRange(blocks);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<IReadOnlyList<BlockResponse>>.Success([.. blocks.Select(BlockResponse.From)]);
    }
}
