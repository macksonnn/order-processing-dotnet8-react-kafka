namespace OrderProcessing.Infrastructure.Persistence.Connections;

internal static class Utc
{
    public static DateTimeOffset FromDatabase(DateTime value)
    {
        if (value.Kind == DateTimeKind.Unspecified)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
        }

        return new DateTimeOffset(value.ToUniversalTime());
    }

    public static DateTimeOffset? FromDatabase(DateTime? value) =>
        value is null ? null : FromDatabase(value.Value);
}
