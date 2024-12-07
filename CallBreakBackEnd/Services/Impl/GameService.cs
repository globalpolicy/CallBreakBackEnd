using CallBreakBackEnd.Models;
using CallBreakBackEnd.Models.Db;
using CallBreakBackEnd.Models.DTO.Output;
using CallBreakBackEnd.Services.Exceptions;
using CallBreakBackEnd.Services.Interfaces;
using CallBreakBackEnd.Utils;
using CallBreakBackEnd.Utils.CardTypes;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace CallBreakBackEnd.Services.Impl
{
	public class GameService : IGameService
	{
		private readonly AppDbContext _dbContext;
		private readonly IClientNotifierService _clientNotifier;

		public GameService(AppDbContext dbContext, IClientNotifierService clientNotifier)
		{
			_dbContext = dbContext;
			_clientNotifier = clientNotifier;
		}

		public async Task StartGame(int roomId)
		{
			Room room = await _dbContext.Rooms
							.Include(room => room.Games)
							.Include(room => room.Players)
						.FirstAsync(room => room.Id == roomId);

			int dealerPlayerId;
			if (room.Games.Count == 0)
			{
				// this is gonna be this room's first game. pick a random dealer
				dealerPlayerId = Randoms.GetRandomElement(room.Players).Id;
			}
			else
			{
				// this room has had a previous game. the dealer should be the next player after the previous game's dealer
				Game previousGame = room.Games.Last();
				int newDealerIndex = (Array.IndexOf(room.Players.Select(player => player.Id).Order().ToArray(), previousGame.DealerPlayerId) + 1) % room.Players.Count;
				Player newDealer = room.Players.ElementAt(newDealerIndex);
				dealerPlayerId = newDealer.Id;

				// deactivate the previous game
				previousGame.IsActive = false;
			}

			// create a new entry in Games table
			Game game = Game.Create(roomId, dealerPlayerId);
			await _dbContext.Games.AddAsync(game);
			await _dbContext.SaveChangesAsync();

			// start the first round for this game
			await StartNewRound(game.Id);
		}

		public async Task<int> StartNewRound(int gameId)
		{
			// determine the most recent round number for this game
			var roundsForTheGame = await _dbContext.Rounds.Where(round => round.GameId == gameId).ToArrayAsync();

			// create a new Round entry
			Round newRound;
			if (!roundsForTheGame.Any())
			{
				// start the first round i.e. RoundNumber=0
				newRound = Round.Create(0, gameId);
				await _dbContext.Rounds.AddAsync(newRound);
				await _dbContext.SaveChangesAsync(); // this is needed here to have access to newRound.Id

				// since this is the first round of the game, deal cards to all players i.e. add relevant entries to Turns table
				await DealCards(newRound.Id);

				// notify all players in the room that cards have been dealt, then each player should call the endpoint to get their hand
				Game game = await _dbContext.Games
						.Include(game => game.Room)
					.SingleAsync(game => game.Id == gameId);

				_clientNotifier.NotifyCardsDealt(game.Room.Uid.ToString());
			}
			else
			{
				// start the next round
				int previousRoundNumber = roundsForTheGame.Select(round => round.RoundNumber).Max();
				newRound = Round.Create(previousRoundNumber + 1, gameId);
				await _dbContext.Rounds.AddAsync(newRound);
				await _dbContext.SaveChangesAsync();

			}

			return newRound.Id;
		}

		public async Task<string> GetPlayersHand(int playerId)
		{
			Turn? turn = await _dbContext.Turns.Where(turn => turn.PlayerId == playerId).OrderByDescending(turn => turn.Id).FirstOrDefaultAsync();
			if (turn == null)
				throw new TurnNotFoundException($"No Turn entry exists for player #{playerId}");
			return turn.HandCards;
		}

		public async Task PlayCard(int playerId, string card)
		{
			// player->room->games->active game->rounds
			Player? player = await _dbContext.Players
				.Where(player => player.Id == playerId)
					.Include(player => player.Room)
						.ThenInclude(room => room.Games)
							.ThenInclude(game => game.Rounds)
					.Include(player => player.Room)
						.ThenInclude(room => room.Players)
				.SingleOrDefaultAsync();
			if (player == null)
				throw new PlayerNotFoundException();

			Game? activeGame = player.Room.Games.Where(game => game.IsActive && !game.IsFinished).SingleOrDefault();
			if (activeGame == null)
				throw new ActiveGameNotFoundException();

			ICollection<Round>? rounds = activeGame.Rounds;
			if (rounds == null || rounds.Count == 0)
				throw new RoundNotFoundException();

			// make sure all the players have declared their scores
			IEnumerable<int> playerIds = player.Room.Players.Select(player => player.Id);
			List<Score> declaredScores = await _dbContext.Scores
				.Where(score => score.GameId == activeGame.Id && playerIds.Contains(score.PlayerId))
				.ToListAsync();

			if (declaredScores.Count != player.Room.Players.Count) // this should be guarded against when declaring scores in the first place
				throw new ScoreException($"Score declaration entries({declaredScores.Count}) don't match the number of players({player.Room.Players.Count})!");

			// this info will determine if a new Round entry should be created or if the game should be ended on this turn and what the running roundId is
			TurnInfo gameState = await DetermineGameState(player.Id);

			if (gameState.NextTurnPlayerId != player.Id)
				throw new OutOfTurnPlayException();

			if (!gameState.ThisPlayersLatestHand.Contains(card))
				throw new NotYourCardToPlayException($"{card} is not yours to play!");

			// reaching here means it actually is this player's turn, and there is at least one Round entry and at least {numberOfPlayers} of entries in the Turns table
			// and the played card is not illegal

			int roundIdForNewTurn = gameState.LatestTurnRoundId; // default for if the running round is not over yet
			if (gameState.IsRunningRoundFinished)
				roundIdForNewTurn = await StartNewRound(activeGame.Id); // create a new round for the game

			string playersLatestHand = await GetPlayersHand(player.Id);
			string playersNewHand = playersLatestHand.Remove(playersLatestHand.IndexOf(card), card.Length); // remove the played card from player's hand

			Turn newTurn = Turn.Create(roundIdForNewTurn, player.Id, playersNewHand, card);
			await _dbContext.Turns.AddAsync(newTurn);
			await _dbContext.SaveChangesAsync();

			if (gameState.TurnsRemainingInRunningRound == 1) // since we just played a turn, if only 1 turn was left for the round before we played, this round is a wrap
			{
				await UpdateRoundWithWinner(gameState.LatestTurnRoundId); // update the completed running round with its rightful winner and increment their score
			}

			if (gameState.TurnsRemainingInTheGame == 1) // since we just played a turn, if only 1 turn was left to game over before we played, this game is a wrap
			{
				await FinishGameAndUpdateFinalScore(activeGame.Id);
			}


		}

		/// <summary>
		/// Determines and returns information on the most recent turn and what the next turn is supposed to be, based on the current state of the DB.
		/// </summary>
		/// <param name="playerId"></param>
		/// <returns></returns>
		public async Task<TurnInfoShort> GetLatestTurnInfo(int playerId)
		{
			TurnInfo turnInfo = await DetermineGameState(playerId);

			// make a new RecentTurnOutcomeInfo object to notify players who played the latest turn and what, and whose turn it is next
			TurnInfoShort recentTurnInfo = new TurnInfoShort()
			{
				PlayerId = turnInfo.LatestTurnPlayerId, // the player who played the turn
				PlayedCard = turnInfo.LatestTurnPlayedCard, // what card they played
				PlayerName = turnInfo.LatestTurnPlayerName, // their name
				GameIsOver = turnInfo.IsGameOver,
				RoundIsOver = turnInfo.TurnsRemainingInRunningRound == 0,
				NextTurnPlayerId = turnInfo.NextTurnPlayerId
			};

			return recentTurnInfo;
		}

		/// <summary>
		/// Updates the given game to finished state, and updates the corresponding Scores table entry with each player's final score
		/// </summary>
		/// <param name="gameId">The game to finish</param>
		/// <returns></returns>
		private async Task FinishGameAndUpdateFinalScore(int gameId)
		{
			// deactivate the game
			Game game = (await _dbContext.Games.FindAsync(gameId))!;
			game.IsFinished = true;

			// get all rounds for the game
			List<Round> roundsForGame = await _dbContext.Rounds.Where(round => round.GameId == gameId).ToListAsync();

			// count which player won how many rounds
			Dictionary<int, int> playerIdScoreDict = new Dictionary<int, int>();
			var groupsByWinner = roundsForGame.GroupBy(round => round.WinnerPlayerId);
			foreach (var group in groupsByWinner)
			{
				if (group.Key is int winnerPlayerId) // this is false for the "deal" rounds that don't have a winner
					playerIdScoreDict[winnerPlayerId] = group.Count();
			}

			// update the Scores table with each player's final score
			List<Score> scoresForGame = await _dbContext.Scores.Where(score => score.GameId == gameId).ToListAsync();
			foreach (Score scoreEntry in scoresForGame)
				scoreEntry.ActualScore = playerIdScoreDict[scoreEntry.PlayerId];

			await _dbContext.SaveChangesAsync();
		}

		/// <summary>
		/// When the running round is determined to be finished, this method should be called to update the round's entry with the winner for the round.
		/// This method then increments the ActualScore column's value for the entry corresponding to the winning PlayerId in the Scores table.
		/// </summary>
		/// <param name="finishedRoundId">The id for the round that has just been finished.</param>
		/// <returns></returns>
		/// <exception cref="RoundNotFoundException"></exception>
		private async Task UpdateRoundWithWinner(int finishedRoundId)
		{
			Round? finishedRound = await _dbContext.Rounds.FindAsync(finishedRoundId);
			if (finishedRound == null)
				throw new RoundNotFoundException();

			List<Tuple<int, string>> playerIdCardList = new List<Tuple<int, string>>();
			List<Turn> turnsInRound = await _dbContext.Turns.Where(turn => turn.RoundId == finishedRoundId).OrderBy(turn => turn.Id).ToListAsync();
			turnsInRound.ForEach(turn => playerIdCardList.Add(new Tuple<int, string>(turn.PlayerId, turn.PlayedCard!)));

			int winnerPlayerId = CardsHelper.CalculateWinnerFromPlayedCards(playerIdCardList);
			finishedRound.WinnerPlayerId = winnerPlayerId; // update the winner

			Score? scoreEntryForPlayer = await _dbContext.Scores.SingleOrDefaultAsync(score => score.PlayerId == winnerPlayerId && finishedRound.GameId == score.GameId);
			if (scoreEntryForPlayer == null)
				throw new ScoreException($"No score entry found for PlayerId({winnerPlayerId}) and GameId({finishedRound.GameId})!");

			scoreEntryForPlayer.ActualScore += 1; // increment the score for the winner

			await _dbContext.SaveChangesAsync();
		}

		/// <summary>
		/// Deals cards to all players of a game. This is the first round (RoundNumber=0) of every game.
		/// This method creates {numberOfPlayers} number of entries in the Turns table, one for each player
		/// </summary>
		/// <param name="roundId">The round id (NOT the round number, which is always 0 for dealing cards) for which to deal cards.</param>
		private async Task DealCards(int roundId)
		{
			Round round = await _dbContext.Rounds
						.Where(round => round.Id == roundId)
							.Include(round => round.Game)
						.FirstAsync();
			Game game = round.Game;
			int dealerPlayerId = game.DealerPlayerId;

			// retrieve the players in this game/room. order's gonna depend on who joined the room first
			ICollection<Player> players = await _dbContext.Rounds
										.Where(round => round.Id == roundId)
											.Include(round => round.Game)
												.ThenInclude(game => game.Room)
													.ThenInclude(room => room.Players)
										.Select(round => round.Game.Room.Players)
										.FirstAsync();

			// generate a hand for each player
			List<string> hands = CardsHelper.GenerateHandsOfCardsAsStrings(players.Count);

			// make entries in the Turns table for each player, starting from the player after the dealer for this game
			int playerIndex = Array.IndexOf(players.Select(player => player.Id).ToArray(), dealerPlayerId);
			for (int i = 0; i < players.Count; i++)
			{
				Player player = players.ElementAt(++playerIndex % players.Count);
				Turn turn = Turn.Create(roundId, player.Id, hands[i], string.Empty);

				await _dbContext.Turns.AddAsync(turn);
			}
			await _dbContext.SaveChangesAsync();

		}

		/// <summary>
		/// Determines and returns information on the most recent turn and what the next turn is supposed to be, based on the current state of the DB.
		/// </summary>
		/// <param name="playerId"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		/// <exception cref="RoundNotFoundException"></exception>
		/// <exception cref="WinnerNotAvailableForRoundException"></exception>
		private async Task<TurnInfo> DetermineGameState(int playerId)
		{
			TurnInfo retval = new TurnInfo();

			// retrieve the players in this game/room. order's gonna depend on who joined the room first
			Player player = await _dbContext.Players
								.Include(player => player.Room)
									.ThenInclude(room => room.Players)
							.FirstAsync(player => player.Id == playerId);
			ICollection<Player> players = player.Room.Players;
			int[] playerIds = players.Select(player => player.Id).Order().ToArray();

			// last turn was this game's player's whose entry is the last in Turns table
			var turnsOfThisGame = await _dbContext.Turns.Where(turn => playerIds.Contains(turn.PlayerId) && turn.Round.Game.IsActive)
				.Include(turn => turn.Player)
				.Include(turn => turn.Round)
					.ThenInclude(round => round.Game)
				.OrderBy(turn => turn.Id).ToListAsync();
			if (turnsOfThisGame == null)
				throw new Exception($"No entry found in Turns table for this game's players!");
			Turn lastTurn = turnsOfThisGame.Last();

			int latestRoundId = lastTurn.RoundId;

			retval.LatestTurnPlayerId = lastTurn.PlayerId;
			retval.LatestTurnPlayerName = lastTurn.Player.Name;
			retval.LatestTurnPlayedCard = lastTurn.PlayedCard;
			retval.LatestTurnPlayerRemainingHand = lastTurn.HandCards;

			// should be true if the existing number of turns for this game is a multiple of the total number of players
			retval.IsRunningRoundFinished = turnsOfThisGame.Count % players.Count == 0;

			// the natural progression of turn for within the same round is: next player = players[(last player's index + 1)%players.count]
			int nextPlayerIdWithinRound = playerIds[(Array.IndexOf(playerIds, lastTurn.PlayerId) + 1) % playerIds.Length];

			// determine the whose turn it is next
			if (!retval.IsRunningRoundFinished)
				retval.NextTurnPlayerId = nextPlayerIdWithinRound;
			else
			{
				// next player is the latest round's winner
				Round? latestRound = await _dbContext.Rounds.FindAsync(latestRoundId);
				if (latestRound == null)
					throw new RoundNotFoundException($"Round id {latestRoundId} not found!");
				if (latestRound.WinnerPlayerId is int winnerOfLatestRound) // the WinnerPlayerId column HAS BEEN assigned a value
					retval.NextTurnPlayerId = winnerOfLatestRound;
				else if (latestRound.RoundNumber == 0) // the WinnerPlayerId column has NOT BEEN assigned a value, and the latest round is "deal" round
					retval.NextTurnPlayerId = nextPlayerIdWithinRound;
				else // the WinnerPlayerId column has NOT BEEN assigned a value, and the latest round is a normal "play" round
					throw new WinnerNotAvailableForRoundException($"No winner found for round id: {latestRoundId}!");
			}

			// should be true if the next turn is gonna be a new round and the last turn of this game resulted in emptying that player's hand
			retval.IsGameOver = retval.IsRunningRoundFinished && string.IsNullOrEmpty(turnsOfThisGame.Last().HandCards.Replace(",", "").Trim());

			retval.LatestTurnRoundId = latestRoundId;

			retval.TurnsRemainingInRunningRound = turnsOfThisGame.Count % players.Count == 0 ? 0 : players.Count - (turnsOfThisGame.Count % players.Count);

			retval.TurnsRemainingInTheGame = CardsHelper.GetDeckSize(players.Count) - (turnsOfThisGame.Count - players.Count); // discount the first {numOfPlayers} turns since they're deal turns

			retval.ThisPlayersLatestHand = turnsOfThisGame.Last(turn => turn.PlayerId == playerId).HandCards; // the current hand cards of the requesting player

			return retval;
		}

	}
}
