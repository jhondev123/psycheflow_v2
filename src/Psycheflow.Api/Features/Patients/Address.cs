using FluentValidation;
using Psycheflow.Api.Common.Validation;

namespace Psycheflow.Api.Features.Patients;

/// <summary>Endereço do paciente. O CEP preenche o restante no front (ViaCEP, RN-26); a API só valida e normaliza.</summary>
public sealed class Address
{
    public const int ZipCodeLength = 8;

    private Address()
    {
    }

    public string ZipCode { get; private set; } = string.Empty;

    public string Street { get; private set; } = string.Empty;

    public string Number { get; private set; } = string.Empty;

    public string? Complement { get; private set; }

    public string Neighborhood { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string State { get; private set; } = string.Empty;

    /// <summary>Converte o DTO já validado (<see cref="AddressValidator"/>) em endereço normalizado.</summary>
    public static Address? From(AddressDto? dto) => dto is null
        ? null
        : new Address
        {
            ZipCode = new string(dto.ZipCode.Where(char.IsAsciiDigit).ToArray()),
            Street = dto.Street.Trim(),
            Number = dto.Number.Trim(),
            Complement = string.IsNullOrWhiteSpace(dto.Complement) ? null : dto.Complement.Trim(),
            Neighborhood = dto.Neighborhood.Trim(),
            City = dto.City.Trim(),
            State = dto.State.Trim().ToUpperInvariant(),
        };

    public AddressDto ToDto() => new(ZipCode, Street, Number, Complement, Neighborhood, City, State);
}

public sealed record AddressDto(
    string ZipCode,
    string Street,
    string Number,
    string? Complement,
    string Neighborhood,
    string City,
    string State);

public sealed class AddressValidator : AbstractValidator<AddressDto>
{
    private static readonly HashSet<string> States = new(StringComparer.OrdinalIgnoreCase)
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA",
        "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
    };

    public AddressValidator()
    {
        RuleFor(x => x.ZipCode)
            .Must(zip => zip is not null
                && zip.All(c => char.IsAsciiDigit(c) || c == '-')
                && zip.Count(char.IsAsciiDigit) == Address.ZipCodeLength)
            .WithMessage("CEP inválido. Informe os 8 dígitos.");
        RuleFor(x => x.Street).RequiredText("a rua", 150);
        RuleFor(x => x.Number).RequiredText("o número", 20);
        RuleFor(x => x.Complement).OptionalText(100);
        RuleFor(x => x.Neighborhood).RequiredText("o bairro", 100);
        RuleFor(x => x.City).RequiredText("a cidade", 100);
        RuleFor(x => x.State)
            .Must(uf => uf is not null && States.Contains(uf.Trim()))
            .WithMessage("UF inválida.");
    }
}
