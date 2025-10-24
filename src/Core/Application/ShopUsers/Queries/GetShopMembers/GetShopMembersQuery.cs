using MediatR;
using pos_system_api.Core.Application.ShopUsers.DTOs;

namespace pos_system_api.Core.Application.ShopUsers.Queries.GetShopMembers;

/// <summary>
/// Query to get all members of a shop
/// </summary>
public record GetShopMembersQuery(
    string ShopId,
    bool ActiveOnly = true
) : IRequest<List<ShopMemberDto>>;
