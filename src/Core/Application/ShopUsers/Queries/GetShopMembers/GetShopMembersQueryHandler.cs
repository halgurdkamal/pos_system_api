using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.ShopUsers.DTOs;

namespace pos_system_api.Core.Application.ShopUsers.Queries.GetShopMembers;

/// <summary>
/// Handler for getting all members of a shop
/// </summary>
public class GetShopMembersQueryHandler : IRequestHandler<GetShopMembersQuery, List<ShopMemberDto>>
{
    private readonly IShopUserRepository _shopUserRepository;
    private readonly IUserRepository _userRepository;
    private readonly IShopRepository _shopRepository;

    public GetShopMembersQueryHandler(
        IShopUserRepository shopUserRepository,
        IUserRepository userRepository,
        IShopRepository shopRepository)
    {
        _shopUserRepository = shopUserRepository;
        _userRepository = userRepository;
        _shopRepository = shopRepository;
    }

    public async Task<List<ShopMemberDto>> Handle(GetShopMembersQuery request, CancellationToken cancellationToken)
    {
        var shopUsers = await _shopUserRepository.GetShopMembersAsync(
            request.ShopId,
            request.ActiveOnly,
            cancellationToken);

        var shop = await _shopRepository.GetByIdAsync(request.ShopId, cancellationToken);

        var members = new List<ShopMemberDto>();

        foreach (var shopUser in shopUsers)
        {
            var user = await _userRepository.GetByIdAsync(shopUser.UserId, cancellationToken);
            if (user != null)
            {
                members.Add(new ShopMemberDto
                {
                    Id = shopUser.Id,
                    UserId = shopUser.UserId,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    ShopId = shopUser.ShopId,
                    ShopName = shop?.ShopName ?? string.Empty,
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

        return members;
    }
}
