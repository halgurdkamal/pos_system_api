using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.ShopUsers.DTOs;

namespace pos_system_api.Core.Application.ShopUsers.Queries.GetUserMemberships;

/// <summary>
/// Handler for getting all shop memberships for a user
/// </summary>
public class GetUserMembershipsQueryHandler : IRequestHandler<GetUserMembershipsQuery, List<ShopMemberDto>>
{
    private readonly IShopUserRepository _shopUserRepository;
    private readonly IUserRepository _userRepository;
    private readonly IShopRepository _shopRepository;

    public GetUserMembershipsQueryHandler(
        IShopUserRepository shopUserRepository,
        IUserRepository userRepository,
        IShopRepository shopRepository)
    {
        _shopUserRepository = shopUserRepository;
        _userRepository = userRepository;
        _shopRepository = shopRepository;
    }

    public async Task<List<ShopMemberDto>> Handle(GetUserMembershipsQuery request, CancellationToken cancellationToken)
    {
        var shopUsers = await _shopUserRepository.GetUserShopsAsync(
            request.UserId,
            request.ActiveOnly,
            cancellationToken);

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        var memberships = new List<ShopMemberDto>();

        foreach (var shopUser in shopUsers)
        {
            var shop = await _shopRepository.GetByIdAsync(shopUser.ShopId, cancellationToken);
            if (shop != null && user != null)
            {
                memberships.Add(new ShopMemberDto
                {
                    Id = shopUser.Id,
                    UserId = shopUser.UserId,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    ShopId = shopUser.ShopId,
                    ShopName = shop.ShopName,
                    Role = shopUser.Role.ToString(),
                    Permissions = shopUser.Permissions.Select(p => p.ToString()).ToList(),
                    IsOwner = shopUser.IsOwner,
                    IsActive = shopUser.IsActive,
                    JoinedDate = shopUser.JoinedDate,
                    InvitedBy = shopUser.InvitedBy,
                    LastAccessDate = shopUser.LastAccessDate,
                    Notes = shopUser.Notes
                });
            }
        }

        return memberships;
    }
}
