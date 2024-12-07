using CallBreakBackEnd.Models;
using CallBreakBackEnd.Models.Db;
using CallBreakBackEnd.Models.DTO.Input;
using CallBreakBackEnd.Models.DTO.Output;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallBreakBackEnd.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PlayerController : BaseController
	{
		private readonly AppDbContext _dbContext;
		private readonly ILogger<PlayerController> _logger;
		public PlayerController(AppDbContext dbContext, ILogger<PlayerController> logger)
		{
			_dbContext = dbContext;
			_logger = logger;
		}

		/// <summary>
		/// This endpoint should be called by a prospective new player. Upon receiving a successful response, a SignalR connection should be started from the client.
		/// </summary>
		/// <param name="joinRequest"></param>
		/// <returns></returns>
		[HttpPost("joinroom")]
		public async Task<IActionResult> JoinRoom([FromBody] PlayerJoinRequest joinRequest)
		{
			try
			{
				if (!Guid.TryParse(joinRequest.RoomUid, out Guid guid))
					return Error(System.Net.HttpStatusCode.Forbidden, "Invalid room id!");

				Room? room = await _dbContext.Rooms
					.Include(room => room.Players)
					.SingleOrDefaultAsync(room => room.Uid == guid);
				if (room == null)
					return Error(System.Net.HttpStatusCode.NotFound, "Room not found!");
				if (!room.Active)
					return Error(System.Net.HttpStatusCode.Forbidden, "Room is not active!");
				if (room.Players.Count >= room.Capacity)
					return Error(System.Net.HttpStatusCode.Forbidden, "Room is full!");


				Player player = Player.Create(joinRequest.PlayerName, room.Id);
				await _dbContext.Players.AddAsync(player);
				await _dbContext.SaveChangesAsync();

				return Ok(
					new
					{
						PlayerName = joinRequest.PlayerName,
						PlayerUid = player.Uid
					});
			}
			catch (InvalidOperationException ioex)
			{
				_logger.LogError(ioex, null);
				return Error(System.Net.HttpStatusCode.InternalServerError, "Duplicate room uids!");
			}
		}

		[HttpGet("getplayerdetails/{playerUid:guid}")]
		public async Task<IActionResult> GetPlayerDetails(string playerUid)
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
					PlayerId = player.Id,
					JoinedAt = player.JoinedAt,
					PlayerName = player.Name,
					PlayerUid = player.Uid,
					IsRoomAdmin = player.Room.AdminUserId == player.UserId,
					RoomInfoShort = new
					{
						Capacity = player.Room.Capacity,
						Occupancy = player.Room.Players.Count,
						RoomUid = player.Room.Uid
					}
				});
			}
			catch (InvalidOperationException ioex)
			{
				_logger.LogError(ioex, null);
				return Error(System.Net.HttpStatusCode.InternalServerError, "Duplicate player uids!");
			}

		}
	}
}
