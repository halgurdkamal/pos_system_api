using MediatR;
using pos_system_api.Core.Application.Auth.DTOs;

namespace pos_system_api.Core.Application.Auth.Commands.Register;

public record RegisterCommand(
    string Username,
    string Email,
    string Password,
    string FullName,
    string? ShopId,
    string Role,
    string? Phone
) : IRequest<UserDto>;
