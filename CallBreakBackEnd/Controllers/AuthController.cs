using CallBreakBackEnd.Models;
using CallBreakBackEnd.Models.Db;
using CallBreakBackEnd.Models.DTO;
using CallBreakBackEnd.Models.DTO.Input;
using CallBreakBackEnd.Models.DTO.Output;
using CallBreakBackEnd.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CallBreakBackEnd.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthController : BaseController
	{
		private readonly ILogger<AuthController> _logger;
		private readonly IConfiguration _configuration;
		private readonly AppDbContext _dbContext;
		private readonly ITokenService _jwtService;
		public AuthController(ILogger<AuthController> logger, IConfiguration configuration, AppDbContext appDbContext, ITokenService jwtService)
		{
			_logger = logger;
			_configuration = configuration;
			_dbContext = appDbContext;
			_jwtService = jwtService;
		}

		/// <summary>
		/// Endpoint to sign on (up and/or in) a user to the app using a Google account.
		/// </summary>
		/// <param name="googleSignOnRequest">An object containing the JWT provided by Google's front-end GSI(Google Sign-In) flow</param>
		/// <returns>A JSON object containing a short-lived access token and a long-lived refresh token for a verified Google account.</returns>
		[HttpPost("googlesignon")]
		public async Task<IActionResult> GoogleSignOn([FromBody] GoogleSignOnRequest googleSignOnRequest)
		{
			try
			{
				GoogleJsonWebSignature.Payload payload;
				try
				{
					payload = await GoogleJsonWebSignature.ValidateAsync(googleSignOnRequest.GoogleJwt, new GoogleJsonWebSignature.ValidationSettings
					{
						Audience = new[] { _configuration["GoogleAppClientId"] }
					});
				}
				catch (Exception ex)
				{
					return Error(System.Net.HttpStatusCode.Unauthorized, "Invalid Google token!");
				}


				if (!payload.EmailVerified)
					return Error(System.Net.HttpStatusCode.Unauthorized, "Google email not verified!");

				User? user = await _dbContext.Users.SingleOrDefaultAsync(user => user.Email == payload.Email);

				// generate new refresh token for this user
				string refreshToken = _jwtService.GenerateRefreshToken();

				if (user == null)
				{
					// add this new user to db
					user = Models.Db.User.Create(payload.Email, refreshToken);
					await _dbContext.Users.AddAsync(user);
					await _dbContext.SaveChangesAsync();
				}

				// generate new access token for this user
				string jwt = _jwtService.GenerateJwt(user.Id);

				// save the generated refresh token and login time to the db for this user
				bool success = await WriteRefreshTokenAndLoggedInTimeToDB(user.Id, refreshToken);
				if (!success)
					throw new Exception($"User id {user.Id} doesn't exist!");

				// return the tokens
				return Ok(new AuthTokens
				{
					AccessToken = jwt,
					RefreshToken = refreshToken
				});

			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Exception on GoogleSignOn");
				return Error(System.Net.HttpStatusCode.InternalServerError);
			}

		}

		/// <summary>
		/// Endpoint to re-issue an access token after verifying the presented refresh token
		/// </summary>
		/// <param name="authTokens"></param>
		/// <returns></returns>
		[HttpPost("refreshaccesstoken")]
		public async Task<IActionResult> RefreshAccessToken([FromBody] AuthTokens authTokens)
		{
			try
			{
				#region Validate given JWT and get userId from it
				Microsoft.IdentityModel.JsonWebTokens.JsonWebToken? validatedToken = await _jwtService.ValidateJwt(authTokens.AccessToken, false); // validate the JWT without considering expiration date
				if (validatedToken == null)
					return Error(System.Net.HttpStatusCode.Unauthorized, "Invalid JWT!");

				Claim claim = validatedToken.GetClaim(ClaimTypes.NameIdentifier);
				int userId = int.Parse(claim.Value);
				#endregion

				User? user = await _dbContext.Users.FindAsync(userId);
				if (user == null)
					return Error(System.Net.HttpStatusCode.NotFound, "Claimed user doesn't exist!");

				if (DateTime.UtcNow - user.LastLoggedInAt > TimeSpan.FromMinutes(_jwtService.RefreshTokenValidityMins))
					return Error(System.Net.HttpStatusCode.Unauthorized, "Refresh token is expired! Please sign in again.");

				if (user.RefreshToken != authTokens.RefreshToken)
					return Error(System.Net.HttpStatusCode.Unauthorized, "Invalid refresh token!");

				return Ok(_jwtService.GenerateJwt(userId));
			}
			catch (Exception ex)
			{
				return Error(System.Net.HttpStatusCode.InternalServerError, "No valid ClaimTypes.NameIdentifier found in the given JWT!");
			}
		}

		/// <summary>
		/// Endpoint typically called to get information useful for loading a registered user's dashboard.
		/// Returns the user's email and the rooms administered
		/// </summary>
		/// <returns></returns>
		[HttpGet("getuserdetails")]
		[Authorize]
		public async Task<IActionResult> GetUserDetails()
		{
			if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
				return Error(System.Net.HttpStatusCode.Unauthorized, "Invalid access token!");

			var user = await _dbContext.Users.FindAsync(userId);
			if (user == null)
				return Error(System.Net.HttpStatusCode.NotFound, "User not found!");

			var roomsAdministrated = await _dbContext.Rooms.Where(room => room.AdminUserId == userId)
						.Include(room => room.Players)
					.ToListAsync();

			var roomsAdministratedInfo = roomsAdministrated.Select(room =>
				new RoomInfo
				{
					Capacity = room.Capacity,
					CreatedAt = room.CreatedAt,
					IsActive = room.Active,
					RoomId = room.Id,
					RoomUid = room.Uid,
					RoomAdminUid = room.Players.SingleOrDefault(player => player.UserId == userId && player.RoomId == room.Id)?.Uid
				}
			).ToArray();



			return Ok(new UserDetails
			{
				Email = user.Email,
				FullName = user.FullName,
				UserName = user.PetName,
				Rooms = roomsAdministratedInfo
			});
		}

		[HttpPost("setprofileinfo")]
		[Authorize]
		public async Task<IActionResult> SaveProfileInfo([FromBody] EditProfileInfo editProfileInfo)
		{
			try
			{
				if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
					return Error(System.Net.HttpStatusCode.Unauthorized, "Invalid access token!");

				var user = await _dbContext.Users.FindAsync(userId);
				if (user == null)
					return Error(System.Net.HttpStatusCode.NotFound, "User not found!");

				user.FullName = editProfileInfo.FullName;
				user.PetName = editProfileInfo.UserName;
				await _dbContext.SaveChangesAsync();

				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.Message);
				return Error(System.Net.HttpStatusCode.InternalServerError);
			}
		}

		/// <summary>
		/// (Over)writes the given refresh token, and the last logged in time in the DB for the given user id
		/// </summary>
		/// <param name="userId">The user id to whom the refresh token belongs.</param>
		/// <param name="refreshToken">The refresh token for the user.</param>
		/// <returns>A bool to indicate if the operation succeeded.</returns>
		private async Task<bool> WriteRefreshTokenAndLoggedInTimeToDB(int userId, string refreshToken)
		{
			User? user = await _dbContext.Users.FindAsync(userId);
			if (user == null)
				return false;
			user.RefreshToken = refreshToken;
			user.LastLoggedInAt = DateTime.UtcNow;
			int changedRows = await _dbContext.SaveChangesAsync();
			return true;

		}
	}
}
