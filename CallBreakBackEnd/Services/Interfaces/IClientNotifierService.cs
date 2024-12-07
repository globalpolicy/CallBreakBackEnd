using CallBreakBackEnd.Models.DTO.Output;

namespace CallBreakBackEnd.Services.Interfaces
{
	public interface IClientNotifierService
	{
		/// <summary>
		/// Notify the relevant group that cards have been dealt for the game.
		/// </summary>
		public void NotifyCardsDealt(string roomUid);

		/// <summary>
		/// Notify the relevant group that scores have changed for the game.
		/// </summary>
		/// <param name="roomUid"></param>
		public void NotifyScoresUpdated(string roomUid);

		/// <summary>
		/// Notify the relevant group that a card was played by a player in the active game, along with information on the consequences of the play.
		/// </summary>
		/// <param name="roomUid"></param>
		/// <param name="recentTurnOutcomeInfo"></param>
		public void NotifyCardPlayed(string roomUid, TurnInfoShort recentTurnOutcomeInfo);
	}
}
