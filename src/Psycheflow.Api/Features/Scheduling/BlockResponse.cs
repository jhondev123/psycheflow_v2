namespace Psycheflow.Api.Features.Scheduling;

public sealed record BlockResponse(Guid Id, Guid PsychologistId, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, string? Reason)
{
    public static BlockResponse From(Schedule block) =>
        new(block.Id, block.PsychologistId, block.Date, block.StartTime, block.EndTime, block.BlockReason);
}
