using MediatR;
using pos_system_api.Core.Application.Shops.DTOs;

namespace pos_system_api.Core.Application.Shops.Commands.CreateOwnShop;

/// <summary>
/// Command for a regular user to create their own shop
/// User becomes the owner of the created shop
/// </summary>
public record CreateOwnShopCommand(
    string UserId,
    string ShopName,
    string PhoneNumber,
    string Email,
    string Address,
    string City,
    string? State,
    string? Country,
    string? PostalCode,
    string? LicenseNumber,
    string? TaxId,
    string? Description
) : IRequest<ShopDto>;
