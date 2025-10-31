using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Sivar.Os.Data.Converters;

/// <summary>
/// Value converter for PostgreSQL vector type stored as string
/// Phase 5: Converts between C# string and PostgreSQL vector type
/// </summary>
public class VectorStringConverter : ValueConverter<string?, string?>
{
    public VectorStringConverter()
        : base(
            // To database: keep as string, PostgreSQL will handle casting via ::vector
            v => v,
            // From database: keep as string
            v => v)
    {
    }
}
