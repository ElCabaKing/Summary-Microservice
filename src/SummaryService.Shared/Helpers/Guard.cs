namespace SummaryService.Shared.Helpers;

public static class Guard
{
    public static void AgainstNull(object? value, string paramName)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
    }

    public static void AgainstEmptyString(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} cannot be empty", paramName);
    }

    public static void AgainstNegative(int value, string paramName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(paramName, $"{paramName} cannot be negative");
    }

    public static void AgainstExceeds(long value, long max, string paramName)
    {
        if (value > max)
            throw new ArgumentOutOfRangeException(paramName, $"{paramName} exceeds maximum of {max}");
    }
}
