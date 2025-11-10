namespace arroyoSeco.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string Generate(string userId, string email, IEnumerable<string> roles, DateTime? expires = null);
}