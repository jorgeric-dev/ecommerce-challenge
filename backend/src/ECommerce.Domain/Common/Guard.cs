using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.Common;

internal static class Guard
{
    public static string RequiredText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return MaxLength(value.Trim(), fieldName, maxLength);
    }

    public static string? OptionalText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return MaxLength(value.Trim(), fieldName, maxLength);
    }

    public static string WithoutMarkup(string value, string fieldName)
    {
        if (value.IndexOfAny(['<', '>']) >= 0)
        {
            throw new DomainException($"{fieldName} must not contain HTML or script markup.");
        }

        return value;
    }

    public static decimal Range(decimal value, string fieldName, decimal min, decimal max, int maxDecimals)
    {
        if (value < min || value > max)
        {
            throw new DomainException($"{fieldName} must be between {min} and {max}.");
        }

        if (decimal.Round(value, maxDecimals) != value)
        {
            throw new DomainException($"{fieldName} must not have more than {maxDecimals} decimal places.");
        }

        return value;
    }

    public static int Range(int value, string fieldName, int min, int max)
    {
        if (value < min || value > max)
        {
            throw new DomainException($"{fieldName} must be between {min} and {max}.");
        }

        return value;
    }

    public static int Positive(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new DomainException($"{fieldName} must be greater than zero.");
        }

        return value;
    }

    private static string MaxLength(string value, string fieldName, int maxLength)
    {
        if (value.Length > maxLength)
        {
            throw new DomainException($"{fieldName} must not exceed {maxLength} characters.");
        }

        return value;
    }
}
