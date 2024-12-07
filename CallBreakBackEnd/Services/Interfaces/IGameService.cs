using CallBreakBackEnd.Models.Db;
using CallBreakBackEnd.Models.DTO.Output;

namespace CallBreakBackEnd.Services.Interfaces
{
	public interface IGameService
	{
		/// <summary>
		/// Creates a new entry in the Games table, then creates a new entry in the Rounds table for the first round, and creates 
		/// {number of players} number of entries in the Turns table with information on the cards dealt. Finally, pushes a notification to all
		/// players in the room that cards have been dealt.
		/// </summary>
		///<param name="roomId">The id of the room in which to start the game.</param>
		/// <returns></returns>
		Task StartGame(int roomId);

		/// <summary>
		/// Creates a new entry in Rounds table with an appropriate value for the RoundNumber column, for the given gameId.
		/// RoundNumber=0 is the first round of every game, which signifies the deal round (cards are dealt to each player).
		/// If it starts the first round, this method also notifies all players in the room that cards have been dealt.
		/// </summary>
		/// <returns>RoundId of the newly created round.</returns>
		Task<int> StartNewRound(int gameId);

		/// <summary>
		/// Retrieves the current hand of the specified player.
		/// </summary>
		/// <param name="playerId"></param>
		/// <returns>A string representation of the player's cards at hand.</returns>
		Task<string> GetPlayersHand(int playerId);

		/// <summary>
		/// Creates a new entry in the Turns table for the specified player with the running round's id if the round is not over; will create a new round and 
		/// use its id for the new Turns entry if the running round is over. Updates the running round with the winner for the round if it's over after creating 
		/// the new Turns entry. Also updates the Scores table with the final score if the game is over after creating the new Turns entry and marks the Game as finished.
		/// </summary>
		/// <param name="playerId"></param>
		/// <param name="card"></param>
		/// <returns></returns>
		Task PlayCard(int playerId, string card);

		/// <summary>
		/// Determines and returns information on the most recent turn and what the next turn is supposed to be, based on the current state of the DB.
		/// </summary>
		/// <param name="playerId"></param>
		/// <returns></returns>
		Task<TurnInfoShort> GetLatestTurnInfo(int playerId);

	}
}
