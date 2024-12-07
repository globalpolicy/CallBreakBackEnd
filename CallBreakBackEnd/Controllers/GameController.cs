using CallBreakBackEnd.Models;
using CallBreakBackEnd.Models.Db;
using CallBreakBackEnd.Models.DTO.Input;
using CallBreakBackEnd.Models.DTO.Output;
using CallBreakBackEnd.Services.Exceptions;
using CallBreakBackEnd.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Numerics;
using System.Security.Claims;

namespace CallBreakBackEnd.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class GameController : BaseController
	{
		private readonly AppDbContext _dbContext;
		private readonly ILogger<GameController> _logger;
		private IGameService _gameService;
		private IClientNotifierService _clientNotifier;

		public GameController(AppDbContext dbContext, ILogger<GameController> logger, IGameService gameService, IClientNotifierService clientNotifier)
		{
			_dbContext = dbContext;
			_logger = logger;
			_gameService = gameService;
			_clientNotifier = clientNotifier;
		}

		/// <summary>
		/// The room admin (their playerUid, actually) calls this endpoint to start a new game after creating a room and after the required no. of players 
		/// have joined the room. The players of the game are the same as players of the room in which it's been started.
		/// Only one game can be active per room at once.
		/// </summary>
		/// <returns></returns>
		[HttpGet("startgame/{adminPlayerUid:guid}")]
		public async Task<IActionResult> StartGame(string adminPlayerUid)
		{

			try
			{
				Player? player = await _dbContext.Players.SingleOrDefaultAsync(player => player.Uid.ToString() == adminPlayerUid);
				if (player == null)
					return Error(System.Net.HttpStatusCode.NotFound, "No player exists for the given playerUid!");
				if (player.UserId == null) // a player UID corresponding to a room admin must have an associated userId
					return Error(System.Net.HttpStatusCode.Forbidden, "Given UID does not belong to a room admin!");

				User? userForPlayer = await _dbContext.Users.FindAsync(player.UserId);
				if (userForPlayer == null)
					return Error(System.Net.HttpStatusCode.NotFound, "No user exists for the given playerUid!");

				Room? room = await _dbContext.Rooms
						.Include(room => room.Players)
					.SingleOrDefaultAsync(room => room.AdminUserId == userForPlayer.Id && room.Active);

				if (room == null)
					return Error(System.Net.HttpStatusCode.NotFound, "Room not found!");

				if (room.Players.Count != room.Capacity)
					return Error(System.Net.HttpStatusCode.BadRequest, "Not enough players!");

				await _gameService.StartGame(room.Id);

				return Ok();


			}
			catch (InvalidOperationException ioex)
			{
				_logger.LogError(ioex, null);
				return Error(System.Net.HttpStatusCode.InternalServerError, "More than one room under your administration!");
			}

		}

		/// <summary>
		/// A player calls this endpoint after receiving a notification that cards have been dealt.
		/// </summary>
		/// <param name="playerUid">The uid of the player requesting to see their hand.</param>
		/// <returns></returns>
		[HttpGet("getmyhand/{playerUid:guid}")]
		public async Task<IActionResult> GetMyHand(string playerUid)
		{
			try
			{
				Player? player = await _dbContext.Players.SingleOrDefaultAsync(player => player.Uid.ToString() == playerUid);

				if (player == null)
					return Error(System.Net.HttpStatusCode.NotFound, "No player exists with the given uid!");

				string playersHand = await _gameService.GetPlayersHand(player.Id);

				return Ok(playersHand);
			}
			catch (InvalidOperationException ex)
			{
				_logger.LogError(ex, "Possible duplicate Player UID.");
				return Error(System.Net.HttpStatusCode.BadRequest, "Duplicate player uid!");
			}
			catch (TurnNotFoundException ex)
			{
				_logger.LogError(ex, $"No turns entry found for playerUid {playerUid}");
				return Error(System.Net.HttpStatusCode.NotFound, $"No turns entry found! Has a game been started by the room admin?");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, null);
				return Error(System.Net.HttpStatusCode.InternalServerError);
			}


		}

		/// <summary>
		/// After cards have been dealt and a client has received a notification that says so, and the /getplayers and /gethand endpoint have been called, the client should 
		/// hit this endpoint to declare their predicted score.
		/// </summary>
		/// <param name="declaredScore"></param>
		/// <returns></returns>
		[HttpPost("declarescore")]
		public async Task<IActionResult> DeclareScore([FromBody] ScoreDeclaration declaredScore)
		{
			try
			{
				Player? player = await _dbContext.Players
									.Include(player => player.Room)
										.ThenInclude(room => room.Games)
								.SingleOrDefaultAsync(player => player.Uid.ToString() == declaredScore.PlayerUid);
				if (player == null)
					return Error(System.Net.HttpStatusCode.NotFound, "No player exists for given UID!");

				Game? game = player.Room.Games.SingleOrDefault(game => game.IsActive && !game.IsFinished); // remember, only one Game can be active in a Room at once
				if (game == null)
					return Error(System.Net.HttpStatusCode.NotFound, "No active game for given player!");

				Score score = Score.Create(player.Id, game.Id, declaredScore.PredictedScore);
				await _dbContext.Scores.AddAsync(score);
				await _dbContext.SaveChangesAsync();

				_clientNotifier.NotifyScoresUpdated(player.Room.Uid.ToString());

				return Ok();

			}
			catch (DbUpdateException duex)
			{
				if (duex.InnerException is PostgresException pgex && pgex.SqlState == PostgresErrorCodes.UniqueViolation)
					return Error(System.Net.HttpStatusCode.BadRequest, "A score entry already exists for the player for the game!");
				else
				{
					_logger.LogError(duex.Message);
					return Error(System.Net.HttpStatusCode.InternalServerError);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
				return Error(System.Net.HttpStatusCode.InternalServerError);
			}

		}

		/// <summary>
		/// This endpoint is to be called by a client when it receives the ScoresUpdated notification so that it can obtain the scores 
		/// (predicted and final) of all players of the game.
		/// </summary>
		/// <param name="playerUid">The requesting player's UID</param>
		/// <returns></returns>
		[HttpGet("getscores/{playerUid:guid}")]
		public async Task<IActionResult> GetScores(string playerUid)
		{
			try
			{
				Player? player = await _dbContext.Players
									.Include(player => player.Room)
										.ThenInclude(room => room.Players)
									.Include(player => player.Room)
										.ThenInclude(room => room.Games)
								.SingleOrDefaultAsync(player => player.Uid.ToString() == playerUid);
				if (player == null)
					return Error(System.Net.HttpStatusCode.NotFound, "No player exists for given UID!");

				Game? activeGame = player.Room.Games.SingleOrDefault(game => game.IsActive);
				if (activeGame == null)
					return Error(System.Net.HttpStatusCode.NotFound, "No active game found!");


				Score[]? scoresForActiveGame = await _dbContext.Scores.Where(score => score.GameId == activeGame.Id).Include(score => score.Player).ToArrayAsync();
				if (scoresForActiveGame == null)
					return Error(System.Net.HttpStatusCode.NotFound, "No score information for your game!");

				ScoreInfo[] scoreInfos = scoresForActiveGame.Select(score => new ScoreInfo
				{
					DeclaredScore = score.DeclaredScore,
					PlayerId = score.PlayerId,
					PlayerName = score.Player.Name,
					ActualScore = score.ActualScore
				}).ToArray();

				return Ok(scoreInfos);

			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
				return Error(System.Net.HttpStatusCode.InternalServerError);
			}
		}


		/// <summary>
		/// This endpoint is to be called by a client whose turn it is (determined the first time after cards have been dealt, by an empty notification - CardsHaveBeenDealt()
		/// which should be followed by a request to /gethand, which returns the relevant info; after the first time, each time this endpoint is hit by any player
		/// of the game, the CardHasBeenPlayed() notification is sent to all clients and this notification contains info on who the next player is among other things)
		/// </summary>
		/// <param name="cardPlayed"></param>
		/// <returns></returns>
		[HttpPost("playcard")]
		public async Task<IActionResult> PlayCard([FromBody] PlayedCard cardPlayed)
		{
			try
			{
				Player? player = await _dbContext.Players.SingleOrDefaultAsync(player => player.Uid.ToString() == cardPlayed.PlayerUid);
				if (player == null)
					return Error(System.Net.HttpStatusCode.NotFound, "No player exists with the given uid!");

				await _gameService.PlayCard(player.Id, cardPlayed.Card); // do the deed

				TurnInfoShort latestTurnInfo = await _gameService.GetLatestTurnInfo(player.Id);
				_clientNotifier.NotifyCardPlayed(player.Room.Uid.ToString(), latestTurnInfo); // notify all players in the room of what just happened

				return Ok(latestTurnInfo);

			}
			catch (PlayerNotFoundException pnfex)
			{
				return Error(System.Net.HttpStatusCode.NotFound, "Player not found!");
			}
			catch (ActiveGameNotFoundException agnfex)
			{
				return Error(System.Net.HttpStatusCode.NotFound, "Active game not found!");
			}
			catch (RoundNotFoundException rnfsex)
			{
				_logger.LogError(rnfsex.Message);
				return Error(System.Net.HttpStatusCode.InternalServerError, "No rounds entry found for this game!");
			}
			catch (ScoreException sdex)
			{
				_logger.LogError(sdex.Message);
				return Error(System.Net.HttpStatusCode.InternalServerError, sdex.Message);
			}
			catch (TurnNotFoundException tnfsex)
			{
				_logger.LogError(tnfsex.Message);
				return Error(System.Net.HttpStatusCode.InternalServerError, "No turns entry found for this round!");
			}
			catch (OutOfTurnPlayException ootpex)
			{
				return Error(System.Net.HttpStatusCode.Forbidden, "It's not your turn!");
			}
			catch (NotYourCardToPlayException nyctpex)
			{
				return Error(System.Net.HttpStatusCode.Forbidden, nyctpex.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
				return Error(System.Net.HttpStatusCode.InternalServerError);
			}
		}

		/// <summary>
		/// Retrieves the latest play corresponding to the latest Turn entry for the active game the requesting player is in.
		/// NOTE: Since this method returns information that's frequently updated (on every turn), a client should only fall back to this endpoint if the
		/// CardHasBeenPlayed() client-side callback isn't triggered by the SignalR connection for any reason. Typically, all clients of a room
		/// will get the latest Turn information through the parameter of the said callback itself after a player has played a card.
		/// </summary>
		/// <param name="playerUid"></param>
		/// <returns></returns>
		[HttpGet("getlatestplay/{playerUid:guid}")]
		public async Task<ActionResult<TurnInfoShort>> GetLatestPlay(string playerUid)
		{
			try
			{
				Player? thisPlayer = await _dbContext.Players.SingleOrDefaultAsync(player => player.Uid.ToString() == playerUid);
				if (thisPlayer == null)
					return Error(System.Net.HttpStatusCode.NotFound, "Player not found!");

				TurnInfoShort latestPlayInfo = await _gameService.GetLatestTurnInfo(thisPlayer.Id);
				return Ok(latestPlayInfo);
			}
			catch (InvalidOperationException ioex)
			{
				_logger.LogError(ioex.Message);
				return Error(System.Net.HttpStatusCode.InternalServerError);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
				return Error(System.Net.HttpStatusCode.InternalServerError);
			}

		}

		/// <summary>
		/// Returns if an active game exists in the room the requesting player belongs to.
		/// </summary>
		/// <param name="playerUid">The requesting player's UID</param>
		/// <returns></returns>
		[HttpGet("doesactivegameexist/{playerUid:guid}")]
		public async Task<ActionResult<bool>> DoesActiveGameExist(string playerUid)
		{
			try
			{
				Player? thisPlayer = await _dbContext.Players
					.Include(player => player.Room)
						.ThenInclude(room => room.Games)
					.SingleOrDefaultAsync(player => player.Uid.ToString() == playerUid);
				if (thisPlayer == null)
					return Error(System.Net.HttpStatusCode.NotFound, "Player not found!");

				bool anyActiveGame = thisPlayer.Room.Games.Any(game => game.IsActive);
				if (anyActiveGame)
					return Ok(true);
				else
					return Ok(false);
			}
			catch (InvalidOperationException ioex)
			{
				_logger.LogError(ioex.Message, "Possible duplicate player Uid.");
				return Error(System.Net.HttpStatusCode.InternalServerError, "Duplicate playerUid!");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
				return Error(System.Net.HttpStatusCode.InternalServerError);
			}
		}

		/// <summary>
		/// Returns the number of cards in hand of all players of the game that the requesting player is playing
		/// </summary>
		/// <param name="playerUid">The requesting player's UID</param>
		/// <returns></returns>
		[HttpGet("getplayershandcounts/{playerUid:guid}")]
		public async Task<ActionResult<Dictionary<int, int>>> GetPlayersHandCounts(string playerUid)
		{
			try
			{
				Player? thisPlayer = await _dbContext.Players
					.Include(player => player.Room)
						.ThenInclude(room => room.Players)
					.SingleOrDefaultAsync(player => player.Uid.ToString() == playerUid);
				if (thisPlayer == null)
					return Error(System.Net.HttpStatusCode.NotFound, "Player not found!");

				var playerIdsOfRoom = thisPlayer.Room.Players.Select(player => player.Id);

				List<Turn> latestTurnsOfPlayers = await _dbContext.Turns.Where(turn => playerIdsOfRoom.Contains(turn.PlayerId))
										.Include(turn => turn.Round)
											.ThenInclude(round => round.Game)
									.OrderByDescending(turn => turn.Id).ToListAsync();
				if (latestTurnsOfPlayers.Count == 0 || !latestTurnsOfPlayers.First().Round.Game.IsActive)
					return Error(System.Net.HttpStatusCode.NotFound, "No turns entry found! Has a game been started by room admin?");

				Dictionary<int, int> playerIdHandDict = new Dictionary<int, int>();
				latestTurnsOfPlayers.ForEach(turn =>
				{
					if (!playerIdHandDict.ContainsKey(turn.PlayerId)) // we only want the latest Turn entry for the player
						playerIdHandDict.Add(turn.PlayerId, turn.HandCards.Split(",", StringSplitOptions.RemoveEmptyEntries).Length);
				});
				return Ok(playerIdHandDict);
			}
			catch (InvalidOperationException ioex)
			{
				_logger.LogError(ioex.Message, "Possible duplicate player Uid.");
				return Error(System.Net.HttpStatusCode.InternalServerError, "Duplicate playerUid!");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
				return Error(System.Net.HttpStatusCode.InternalServerError);
			}
		}

		/// <summary>
		/// Retrieves a list of PlayedCardInfo for the active round. The ordering is important to signify which card was played first.
		/// </summary>
		/// <param name="playerUid"></param>
		/// <returns></returns>
		[HttpGet("getplayedcards/{playerUid:guid}")]
		public async Task<ActionResult<List<PlayedCardInfo>>> GetPlayedCards(string playerUid)
		{
			try
			{
				Player? thisPlayer = await _dbContext.Players
					.Include(player => player.Room)
						.ThenInclude(room => room.Players)
					.Include(player => player.Room)
						.ThenInclude(room => room.Games)
							.ThenInclude(game => game.Rounds)
								.ThenInclude(round => round.Turns)
					.SingleOrDefaultAsync(player => player.Uid.ToString() == playerUid);
				if (thisPlayer == null)
					return Error(System.Net.HttpStatusCode.NotFound, "Player not found!");

				List<Game> activeGames = thisPlayer.Room.Games.Where(game => game.IsActive).ToList();
				if (activeGames.Count == 0)
					return Error(System.Net.HttpStatusCode.NotFound, "No active game found! Has the room admin started one?");
				if (activeGames.Count > 1)
				{
					_logger.LogError($"Duplicate active games found for player {thisPlayer.Uid} of room {thisPlayer.Room.Uid}");
					return Error(System.Net.HttpStatusCode.InternalServerError, "More than one active game in this room!");
				}

				Game activeGame = activeGames.First();
				Round? latestRound = activeGame.Rounds.OrderByDescending(round => round.Id).FirstOrDefault();
				if (latestRound == null)
				{
					_logger.LogError($"No rounds entry found for active gameId: {activeGame.Id}");
					return Error(System.Net.HttpStatusCode.InternalServerError, "No round entry found for active game!");
				}

				List<Turn> turnsOfLatestRound = latestRound.Turns.Where(turn => turn.RoundId == latestRound.Id).OrderBy(turn => turn.Id).ToList();
				if (turnsOfLatestRound.Count == 0)
				{
					_logger.LogError($"No turns entries found for active game's roundId: {latestRound.Id}");
					return Error(System.Net.HttpStatusCode.InternalServerError, $"No turns entries found for active game's roundId: {latestRound.Id}!");
				}

				List<PlayedCardInfo> playedCards = new List<PlayedCardInfo>();
				foreach (Turn turn in turnsOfLatestRound)
				{
					playedCards.Add(new PlayedCardInfo
					{
						PlayerId = turn.PlayerId,
						Card = turn.PlayedCard ?? ""
					});
				}
				return Ok(playedCards);
			}
			catch (InvalidOperationException ioex)
			{
				_logger.LogError(ioex.Message, "Possible duplicate player Uid.");
				return Error(System.Net.HttpStatusCode.InternalServerError, "Duplicate playerUid!");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
				return Error(System.Net.HttpStatusCode.InternalServerError);
			}
		}
	}
}
