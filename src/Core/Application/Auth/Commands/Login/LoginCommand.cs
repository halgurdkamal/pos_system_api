using MediatR;
using pos_system_api.Core.Application.Auth.DTOs;

namespace pos_system_api.Core.Application.Auth.Commands.Login;

/// <summary>
/// Login command that accepts username, email, or phone number as identifier
/// </summary>
public record LoginCommand(string Identifier, string Password) : IRequest<TokenResponseDto>;
