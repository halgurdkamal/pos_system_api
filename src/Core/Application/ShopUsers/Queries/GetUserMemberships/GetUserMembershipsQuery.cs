using MediatR;
using pos_system_api.Core.Application.ShopUsers.DTOs;

namespace pos_system_api.Core.Application.ShopUsers.Queries.GetUserMemberships;

/// <summary>
/// Query to get all shop memberships for a user
/// </summary>
public record GetUserMembershipsQuery(
    string UserId,
    bool ActiveOnly = true
) : IRequest<List<ShopMemberDto>>;
