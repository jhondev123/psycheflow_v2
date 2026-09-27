namespace Psycheflow.Api.Features.Users.CreateUser;

/// <param name="TemporaryPassword">Mostrada uma única vez; o usuário é obrigado a trocá-la no primeiro acesso.</param>
public sealed record CreateUserResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    Guid? PsychologistId,
    string TemporaryPassword);
