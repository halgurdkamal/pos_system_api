using System.Security.Claims;
using MediatR;
using pos_system_api.Core.Application.Auth.DTOs;

namespace pos_system_api.Core.Application.Auth.Queries.GetCurrentUser;

/// <summary>
/// Reads the authenticated user's identity from a <see cref="ClaimsPrincipal"/>
/// (typically from the JWT) and projects it into a <see cref="UserDto"/>.
/// Returns null when no user-id claim is present.
/// </summary>
public record GetCurrentUserQuery(ClaimsPrincipal Principal) : IRequest<UserDto?>;
