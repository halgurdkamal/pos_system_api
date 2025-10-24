using MediatR;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Shops.DTOs;
using pos_system_api.Core.Domain.Shops.Entities;

namespace pos_system_api.Core.Application.Shops.Queries.GetAllShops;

/// <summary>
/// Query to get all shops with pagination and optional status filtering
/// </summary>
public record GetAllShopsQuery(
    int Page = 1, 
    int Limit = 20, 
    ShopStatus? Status = null
) : IRequest<PagedResult<ShopDto>>;
