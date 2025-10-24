using MediatR;
using pos_system_api.Core.Application.Shops.DTOs;

namespace pos_system_api.Core.Application.Shops.Commands.UpdateShop;

/// <summary>
/// Command to update shop's receipt configuration only
/// </summary>
public record UpdateReceiptConfigCommand(string ShopId, UpdateReceiptConfigDto ReceiptConfig) : IRequest<ShopDto>;
