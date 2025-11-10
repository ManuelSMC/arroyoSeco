using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using arroyoSeco.Application.Common.Interfaces;

namespace arroyoSeco.Infrastructure.Auth;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;
    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    public string UserId => _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anon";

    public bool IsInRole(string role) => _http.HttpContext?.User.IsInRole(role) ?? false;
}