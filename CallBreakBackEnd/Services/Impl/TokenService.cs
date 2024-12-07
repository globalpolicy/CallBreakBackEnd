using CallBreakBackEnd.Services.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CallBreakBackEnd.Services.Impl
{
    public class TokenService : ITokenService
    {
        private IConfiguration _configuration;
        private string _jwtSecret;
        private string _issuer;
        private string _audience;
        private int _accessTokenValidityMins;
        private int _refreshTokenValidityMins;
        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
            _jwtSecret = _configuration["JWT:PrivateSigningKey"] ?? throw new ArgumentNullException("JWT secret key is empty.");
            _issuer = _configuration["JWT:Issuer"] ?? throw new ArgumentException("JWT issuer is empty.");
            _audience = _configuration["JWT:Audience"] ?? throw new ArgumentException("JWT audience is empty.");
            _accessTokenValidityMins = _configuration.GetValue("JWT:AccessTokenValidityMinutes", 30);
            _refreshTokenValidityMins = _configuration.GetValue("JWT:RefreshTokenValidityMinutes", 43200);
        }

        public int RefreshTokenValidityMins { get => _refreshTokenValidityMins; }
        public int AccessTokenValidityMins { get => _accessTokenValidityMins; }
        public string Audience { get => _audience; }
        public string Issuer { get => _issuer; }
        public string JwtSecret { get => _jwtSecret; }

        /// <summary>
        /// Generate a JWT for the given user id
        /// </summary>
        /// <param name="userId">The user id to put as a claim in the JWT</param>
        /// <returns>A signed JWT for the given user id</returns>
        public string GenerateJwt(int userId)
        {
            SymmetricSecurityKey symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            SigningCredentials signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            Claim[] claims = { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }; // create a single element array of a claim for the user id
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(issuer: _issuer, audience: _audience, notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_accessTokenValidityMins), signingCredentials: signingCredentials,
                claims: claims);
            return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        }

        /// <summary>
        /// Generate a refresh token (just a GUID)
        /// </summary>
        /// <returns>The refresh token generated.</returns>
        public string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Validates the provided JWT and returns the validation result
        /// </summary>
        /// <param name="jwt">The JWT to validate</param>
        /// <param name="checkExpiration">Whether or not the lifetime of the token should be checked</param>
        /// <returns>The TokenValidationResult for the given JWT.</returns>
        public async Task<JsonWebToken?> ValidateJwt(string jwt, bool checkExpiration = true)
        {
            TokenValidationResult validationResult = await new JsonWebTokenHandler().ValidateTokenAsync(jwt, new TokenValidationParameters
            {
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret)),
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = checkExpiration,
            });

            if (validationResult.IsValid)
                return new JsonWebTokenHandler().ReadJsonWebToken(jwt);
            else
                return null;
        }
    }
}
