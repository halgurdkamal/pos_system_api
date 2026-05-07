using MediatR;

namespace pos_system_api.Core.Application.Admin.Queries.GetDatabaseStats;

/// <summary>
/// Returns row counts for the major entity tables. Useful for sanity-checking seed runs
/// and as a smoke-test endpoint.
/// </summary>
public record GetDatabaseStatsQuery : IRequest<DatabaseStatsDto>;

public record DatabaseStatsDto(
    int Drugs,
    int Shops,
    int Suppliers,
    int Inventory,
    int Users,
    DateTime Timestamp);
