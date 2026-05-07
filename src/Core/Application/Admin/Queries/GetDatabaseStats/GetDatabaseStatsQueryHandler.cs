using MediatR;
using Microsoft.EntityFrameworkCore;
using pos_system_api.Infrastructure.Data;

namespace pos_system_api.Core.Application.Admin.Queries.GetDatabaseStats;

public class GetDatabaseStatsQueryHandler : IRequestHandler<GetDatabaseStatsQuery, DatabaseStatsDto>
{
    private readonly ApplicationDbContext _context;

    public GetDatabaseStatsQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DatabaseStatsDto> Handle(
        GetDatabaseStatsQuery request,
        CancellationToken cancellationToken)
    {
        return new DatabaseStatsDto(
            Drugs: await _context.Drugs.CountAsync(cancellationToken),
            Shops: await _context.Shops.CountAsync(cancellationToken),
            Suppliers: await _context.Suppliers.CountAsync(cancellationToken),
            Inventory: await _context.ShopInventory.CountAsync(cancellationToken),
            Users: await _context.Users.CountAsync(cancellationToken),
            Timestamp: DateTime.UtcNow);
    }
}
