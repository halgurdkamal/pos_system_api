using MediatR;
using pos_system_api.Core.Application.Auth.DTOs;

namespace pos_system_api.Core.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<TokenResponseDto>;
