using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using pos_system_api.Infrastructure.Data;

namespace POS.Tests.Unit.Infrastructure;

/// <summary>
/// Builds an in-memory <see cref="ApplicationDbContext"/> for tests.
/// Replaces Postgres-specific jsonb columns (anything stored as JSON in production —
/// List&lt;T&gt;, Dictionary&lt;K,V&gt;, complex value objects) with generic JSON-string
/// converters so the InMemory provider can validate the model.
/// </summary>
internal static class TestDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        return new TestApplicationDbContext(options);
    }

    private sealed class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (NeedsJsonConversion(property.ClrType))
                    {
                        property.SetValueConverter(BuildJsonConverter(property.ClrType));
                        property.SetColumnType(null);
                    }
                }
            }
        }

        private static bool NeedsJsonConversion(Type clrType)
        {
            if (clrType.IsPrimitive || clrType == typeof(string) || clrType == typeof(decimal)
                || clrType == typeof(DateTime) || clrType == typeof(DateTime?)
                || clrType == typeof(Guid) || clrType == typeof(Guid?)
                || clrType.IsEnum)
            {
                return false;
            }

            // Anything else stored on a property (lists, dictionaries, complex value objects)
            // is jsonb in production; needs JSON conversion for InMemory.
            return clrType != typeof(byte[]);
        }

        private static readonly MethodInfo BuildConverterGeneric =
            typeof(TestApplicationDbContext).GetMethod(
                nameof(BuildJsonConverterGeneric),
                BindingFlags.NonPublic | BindingFlags.Static)!;

        private static ValueConverter BuildJsonConverter(Type clrType) =>
            (ValueConverter)BuildConverterGeneric.MakeGenericMethod(clrType).Invoke(null, null)!;

        private static ValueConverter<T, string> BuildJsonConverterGeneric<T>() =>
            new(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v)
                    ? default!
                    : JsonSerializer.Deserialize<T>(v, (JsonSerializerOptions?)null)!);
    }
}
