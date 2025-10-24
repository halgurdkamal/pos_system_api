using MediatR;
using pos_system_api.Core.Application.Shops.DTOs;

namespace pos_system_api.Core.Application.Shops.Commands.UpdateShop;

/// <summary>
/// Command to update an existing shop's basic information
/// </summary>
public record UpdateShopCommand(string ShopId, UpdateShopDto ShopData) : IRequest<ShopDto>;
