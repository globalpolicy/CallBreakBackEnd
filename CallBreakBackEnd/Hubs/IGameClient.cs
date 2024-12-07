using CallBreakBackEnd.Models.DTO.Output;

namespace CallBreakBackEnd.Hubs
{
	/// <summary>
	/// This interface outlines the methods that the client side must implement to enable server->client invocations.
	/// </summary>
	public interface IGameClient
	{
		/// <summary>
		/// Client-side method invoked from server to notify that cards have been dealt to all players upon the start of a game.
		/// </summary>
		/// <returns></returns>
		Task CardsHaveBeenDealt();

		/// <summary>
		/// Client-side method invoked from server to notify a user has declared a score so that the relevant Scores table entry has been updated.
		/// </summary>
		/// <returns></returns>
		Task ScoresUpdated();

		/// <summary>
		/// Client-side method invoked from server to notify that at least one player has played a card.
		/// </summary>
		/// <param name="playedCardInfo">Information on who made the play and what card, who's up next, etc.</param>
		/// <returns></returns>
		Task CardHasBeenPlayed(TurnInfoShort playedCardInfo);

		/// <summary>
		/// Client-side method invoked from server to notify all players of a group/room that a player has joined the room (i.e. come online and has a connection with the server).
		/// </summary>
		/// <returns></returns>
		Task PlayerCameOnline(PlayerInfo playerInfo);

		/// <summary>
		/// Client-side method invoked from server to notify all players of a group/room that a player has left the room (i.e. gone offline with possibility to come back online)
		/// </summary>
		/// <returns></returns>
		Task PlayerWentOffline(PlayerInfo playerInfo);
	}
}
