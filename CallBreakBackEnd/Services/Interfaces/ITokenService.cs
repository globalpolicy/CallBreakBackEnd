using Microsoft.IdentityModel.JsonWebTokens;
using System.IdentityModel.Tokens.Jwt;

namespace CallBreakBackEnd.Services.Interfaces
{
    public interface ITokenService
    {
        #region Properties
        int RefreshTokenValidityMins { get; }
        int AccessTokenValidityMins { get; }
        string Audience { get; }
        string Issuer { get; }
        string JwtSecret { get; }
        #endregion

        #region Methods
        string GenerateJwt(int userId);
        string GenerateRefreshToken();
        Task<JsonWebToken?> ValidateJwt(string jwt, bool checkExpiration = true);
        #endregion
    }
}
