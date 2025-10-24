using MediatR;
using pos_system_api.Core.Application.Shops.DTOs;

namespace pos_system_api.Core.Application.Shops.Commands.RegisterShop;

/// <summary>
/// Command to register a new shop in the system
/// </summary>
public record RegisterShopCommand(CreateShopDto ShopData) : IRequest<ShopDto>;
