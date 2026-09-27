namespace Psycheflow.Api.IntegrationTests.Infrastructure;

/// <summary>Relógio controlável da API nos testes (pode avançar e ser reiniciado entre testes).</summary>
public sealed class TestClock(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => UtcNow;

    public void Advance(TimeSpan duration) => UtcNow += duration;

    /// <summary>Define o "agora" a partir de um horário local de São Paulo (UTC-3).</summary>
    public void SetLocal(DateOnly date, TimeOnly time) =>
        UtcNow = new DateTimeOffset(date.ToDateTime(time), TimeSpan.FromHours(-3)).ToUniversalTime();
}
