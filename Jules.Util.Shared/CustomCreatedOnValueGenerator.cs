using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Jules.Util.Shared;

public class CustomCreatedOnValueGenerator : ValueGenerator<DateTimeOffset>
{
    public override DateTimeOffset Next(EntityEntry entry)
    {
        return DateTimeOffset.UtcNow;  // Or use custom logic to generate a timestamp
    }

    public override bool GeneratesTemporaryValues => false;  // We don't want to generate temporary values
}