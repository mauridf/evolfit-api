using System.Text.RegularExpressions;

namespace EvolFit.Core.ValueObjects;

/// <summary>
/// Value Object que encapsula a validação de e-mail (VALID-004).
/// Imutável — uma vez criado, o valor não muda.
/// </summary>
public sealed record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>
    /// Cria um Email validado. Lança ArgumentException se o formato for inválido.
    /// </summary>
    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("E-mail é obrigatório.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > 255)
            throw new ArgumentException("E-mail deve ter no máximo 255 caracteres.", nameof(value));

        if (!EmailRegex.IsMatch(normalized))
            throw new ArgumentException($"E-mail inválido: {value}", nameof(value));

        return new Email(normalized);
    }

    public override string ToString() => Value;
}
