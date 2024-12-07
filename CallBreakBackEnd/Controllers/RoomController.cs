using CallBreakBackEnd.Models;
using CallBreakBackEnd.Models.Db;
using CallBreakBackEnd.Models.DTO.Output;
using CallBreakBackEnd.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CallBreakBackEnd.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class RoomController : BaseController
	{
		private readonly AppDbContext _dbContext;
		private readonly ILogger<RoomController> _logger;
		private readonly IConnectionIdPlayerMapperService _playerMapperService;
		public RoomController(AppDbContext dbContext, ILogger<RoomController> logger, IConnectionIdPlayerMapperService playerMapperService)
		{
			_dbContext = dbContext;
			_logger = logger;
			_playerMapperService = playerMapperService;
		}


		/// <summary>
		/// Creates a room of specified size. Only callable by authorized users.
		/// </summary>
		/// <param name="noOfPlayers"></param>
		/// <returns></returns>
		[Authorize]
		[HttpPost("createroom")]
		public async Task<IActionResult> CreateRoom([FromBody] int noOfPlayers)
		{
			if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
				return Error(System.Net.HttpStatusCode.Unauthorized, "Invalid access token!");

			// First, de-activate all other rooms created by this user
			await _dbContext.Rooms.Where(room => room.AdminUserId == userId && room.Active)
				.ExecuteUpdateAsync(room => room.SetProperty(r => r.Active, false));

			// add a new entry in Rooms table
			Room room = Room.Create(userId, noOfPlayers);
			await _dbContext.Rooms.AddAsync(room);
			await _dbContext.SaveChangesAsync();

			// add an entry for self in the Players table
			Player self = Player.Create("Room Admin", room.Id);
			self.UserId = userId; // this is non-null for an authenticated user i.e. us
			await _dbContext.Players.AddAsync(self);
			await _dbContext.SaveChangesAsync();

			return CreatedAtAction(nameof(GetRoomDetails), new
			{
				RoomId = room.Id,
				RoomUid = room.Uid
			});
		}

		/// <summary>
		/// Retrieves the details of the active room that the caller is an admin of.
		/// </summary>
		/// <returns></returns>
		[Authorize]
		[HttpGet("getroomdetails")]
		public async Task<IActionResult> GetRoomDetails()
		{
			if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
				return Error(System.Net.HttpStatusCode.Unauthorized, "Invalid access token!");

			try
			{
				Room? room = await _dbContext.Rooms.Include(room => room.Players)
							.SingleOrDefaultAsync(room => room.AdminUserId == userId && room.Active);

				if (room == null)
					return Error(System.Net.HttpStatusCode.NotFound, "No active room found!");

				return Ok(new
				{
					Capacity = room.Capacity,
					RoomUid = room.Uid,
					Players = room.Players.Select(player => new
					{
						JoinedAt = player.JoinedAt,
						PlayerName = player.Name,
					}).ToArray()
				});
			}
			catch (InvalidOperationException ioex)
			{
				_logger.LogError(ioex, null);
				return Error(System.Net.HttpStatusCode.InternalServerError, "More than one room under your administration!");
			}

		}

		/// <summary>
		/// Retrieves the player id and name of all the players in the room the requesting player belongs to.
		/// NOTE: Other players' UIDs are sensitive as they alone are used to identify players, opening the possibility of impersonated requests.
		/// </summary>
		/// <param name="playerUid">The requesting player's UID</param>
		/// <returns></returns>
		[HttpGet("getroomplayers/{playerUid:guid}")]
		public async Task<IActionResult> GetRoomPlayers(string playerUid)
		{
			if (!Guid.TryParse(playerUid, out Guid guid))
				return Error(System.Net.HttpStatusCode.Forbidden, "Invalid player uid!");

			try
			{
				Player? player = await _dbContext.Players
					.Include(player => player.Room)
						.ThenInclude(room => room.Players)
					.SingleOrDefaultAsync(player => player.Uid == guid);
				if (player == null)
					return Error(System.Net.HttpStatusCode.NotFound, "Player not found!");

				return Ok(new
				{
					Players = player.Room.Players.Select(player => new
					{
						JoinedAt = player.JoinedAt,
						PlayerId = player.Id,
						PlayerName = player.Name,
						IsOnline = _playerMapperService.IsPlayerOnline(player.Uid.ToString())
					}).ToArray()
				});
			}
			catch (InvalidOperationException ex)
			{
				_logger.LogError(ex, "Possible duplicate Player UID.");
				return Error(System.Net.HttpStatusCode.BadRequest, "Duplicate player UID!");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, null);
				return Error(System.Net.HttpStatusCode.InternalServerError);
			}
		}


		/// <summary>
		/// Gets the scores (declared and actual) of all players of all games of the requested room
		/// </summary>
		/// <param name="roomUid">The room whose games' scores is to be requested.</param>
		/// <returns></returns>
		[HttpGet("getgamewisescores/{roomUid:guid}")]
		public async Task<IActionResult> GetGameWiseScores(string roomUid)
		{
			try
			{
				Room? room = await _dbContext.Rooms
										.Include(room => room.Players)
										.Include(room => room.Games)
								.SingleOrDefaultAsync(room => room.Uid.ToString() == roomUid);
				if (room == null)
					return Error(System.Net.HttpStatusCode.NotFound, "No room exists for given UID!");

				List<Game> games = room.Games.ToList();
				if (games.Count == 0)
					return Error(System.Net.HttpStatusCode.NotFound, "No game found in room!");

				int[] gameIds = games.Select(game => game.Id).ToArray();

				IGrouping<int, Score>[] scoresGroupedByGameId = await _dbContext.Scores.Where(score => gameIds.Contains(score.GameId))
					.Include(score => score.Player)
					.GroupBy(score => score.GameId).ToArrayAsync();

				var scoreInfos = scoresGroupedByGameId.Select(gameScore => new
				{
					GameId = gameScore.Key,
					GameScores = gameScore.Select(score =>
						new ScoreInfo
						{
							PlayerId = score.PlayerId,
							PlayerName = score.Player.Name,
							DeclaredScore = score.DeclaredScore,
							ActualScore = score.ActualScore
						}
					).ToArray()
				}).ToArray();

				return Ok(scoreInfos);

			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
				return Error(System.Net.HttpStatusCode.InternalServerError);
			}
		}
	}
}
