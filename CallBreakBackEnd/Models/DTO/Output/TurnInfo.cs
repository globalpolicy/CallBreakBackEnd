namespace CallBreakBackEnd.Models.DTO.Output
{
	public struct TurnInfo
	{
		public int LatestTurnPlayerId { get; set; } // who played the most recent turn
		public string LatestTurnPlayerName { get; set; } // name of the player who played the most recent turn
		public string? LatestTurnPlayedCard { get; set; } // what card the most recent player played
		public string LatestTurnPlayerRemainingHand { get; set; } // what hand the player who played the most recent has
		public int NextTurnPlayerId { get; set; } // who's up next
		public bool IsRunningRoundFinished { get; set; }
		public int TurnsRemainingInRunningRound { get; set; }
		public bool IsGameOver { get; set; }
		public int LatestTurnRoundId { get; set; }
		public int TurnsRemainingInTheGame { get; set; }
		public string ThisPlayersLatestHand { get; set; } // the most current hand of the player for whom this struct is returned
	}
}
