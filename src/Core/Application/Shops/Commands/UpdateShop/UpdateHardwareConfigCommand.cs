using MediatR;
using pos_system_api.Core.Application.Shops.DTOs;

namespace pos_system_api.Core.Application.Shops.Commands.UpdateShop;

/// <summary>
/// Command to update shop's hardware configuration only
/// </summary>
public record UpdateHardwareConfigCommand(string ShopId, UpdateHardwareConfigDto HardwareConfig) : IRequest<ShopDto>;
