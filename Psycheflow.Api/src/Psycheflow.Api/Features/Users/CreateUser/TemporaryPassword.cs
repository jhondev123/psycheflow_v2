using System.Security.Cryptography;

namespace Psycheflow.Api.Features.Users.CreateUser;

/// <summary>Senha temporária aleatória (criptograficamente segura) que atende à política de senha (RN-11).</summary>
public static class TemporaryPassword
{
    public const int Length = 12;

    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%&*?-_+=";

    public static string Generate()
    {
        const string all = Upper + Lower + Digits + Symbols;

        char[] password =
        [
            Pick(Upper),
            Pick(Lower),
            Pick(Digits),
            Pick(Symbols),
            .. RandomNumberGenerator.GetItems<char>(all, Length - 4),
        ];

        RandomNumberGenerator.Shuffle<char>(password);
        return new string(password);
    }

    private static char Pick(string source) => source[RandomNumberGenerator.GetInt32(source.Length)];
}
