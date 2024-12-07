using CallBreakBackEnd.Models;
using CallBreakBackEnd.Models.Db;
using CallBreakBackEnd.Models.DTO.Input;
using CallBreakBackEnd.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CallBreakBackEnd.Hubs
{
	/// <summary>
	/// The central hub class for realtime communications between clients and this application.
	/// As of right now, this realtime connection is only used to push notifications from server to clients.
	/// The only time a client executes code on the server is to sign up for notifications after actually connecting to this hub.
	/// This is so for the simple reason that the client's identity has to be determined somehow, and no authentication scheme has been used.
	/// </summary>
	public class GameHub : Hub<IGameClient>
	{
		private readonly IConnectionIdPlayerMapperService _connectionIdMapper;
		private readonly ILogger<GameHub> _logger;
		private readonly AppDbContext _dbContext;
		public GameHub(IConnectionIdPlayerMapperService connectionIdMapper, ILogger<GameHub> logger, AppDbContext dbContext)
		{
			_connectionIdMapper = connectionIdMapper;
			_logger = logger;
			_dbContext = dbContext;
		}

		public override async Task OnConnectedAsync()
		{
			string log = $"New SignalR connection: {Context.ConnectionId}";

			// if this connection is already registered
			if (!string.IsNullOrEmpty(_connectionIdMapper.GetPlayerUid(Context.ConnectionId)))
				log = $"Duplicate connection request for player id {_connectionIdMapper.GetPlayerUid(Context.ConnectionId)}, connection id: {Context.ConnectionId}";

			_logger.LogInformation(log);

			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			string log = $"SignalR client disconnected: {Context.ConnectionId}";
			_logger.LogInformation(log);

			string playerUid = _connectionIdMapper.GetPlayerUid(Context.ConnectionId);

			// if this connection belongs to a previously registered player (i.e. they had invoked the EnablePushNotifications method)
			if (!string.IsNullOrEmpty(playerUid))
			{
				if (_connectionIdMapper.NumberOfConnectionsForPlayer(playerUid) == 1)
				{
					// extract which room this player belongs to
					Player? player = await _dbContext.Players
							.Include(player => player.Room)
						.SingleOrDefaultAsync(player => player.Uid.ToString() == playerUid);
					if (player == null)
						return;
					string roomUid = player.Room.Uid.ToString();

					// let all players in the room know that the player corresponding to this connection has gone offline
					Clients.Group(roomUid).PlayerWentOffline(new Models.DTO.Output.PlayerInfo
					{
						Id = player.Id,
						Name = player.Name,
					});


				}
				_connectionIdMapper.RemoveClient(Context.ConnectionId); // remove this connection from the online player dict

			}

			await base.OnDisconnectedAsync(exception);
		}

		/// <summary>
		/// Method to be called from client-side when the player has successfully connected to this hub.
		/// Upon completion of this method, the client can rest assured that it has successfully "authenticated" with this server application, and as such, will receive all the relevant server->client notifications.
		/// </summary>
		/// <param name="hubConRequest">An object encapsulating properties such as the joining player's UID</param>
		public async Task EnablePushNotifications(HubConnectionRequest hubConRequest)
		{
			string log = $"SignalR client registered: {Context.ConnectionId}";
			_logger.LogInformation(log);

			// extract which room this player belongs to
			Player? player = await _dbContext.Players
					.Include(player => player.Room)
				.SingleOrDefaultAsync(player => player.Uid.ToString() == hubConRequest.PlayerUid);
			if (player == null)
				return;
			string roomUid = player.Room.Uid.ToString();

			// check if this player already has a live connection
			if (!_connectionIdMapper.IsPlayerOnline(hubConRequest.PlayerUid))
			{
				// add this connection id and the calling player UID to the online player dict
				_connectionIdMapper.AddPlayer(Context.ConnectionId, hubConRequest.PlayerUid);

				// let all players in the room know that this player has come online
				Clients.Group(roomUid).PlayerCameOnline(new Models.DTO.Output.PlayerInfo
				{
					Id = player.Id,
					Name = player.Name,
				});

				// add this connection to the group whose name is the room's UID where this player belongs
				await Groups.AddToGroupAsync(Context.ConnectionId, roomUid);



			}
			else
			{
				_logger.LogInformation($"Duplicate EnablePushNotification request for player {hubConRequest.PlayerUid}, connection id: {Context.ConnectionId}");

			}


		}


	}
}
