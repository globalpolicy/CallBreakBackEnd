namespace CallBreakBackEnd.Models.DTO.Output
{
	public struct TurnInfoShort
	{
		public int PlayerId { get; set; }
		public string PlayerName { get; set; }
		public string? PlayedCard { get; set; }
		public bool RoundIsOver { get; set; } // clients should request to get the latest score /getscore if this is true
		public bool GameIsOver { get; set; } // same here
		public int NextTurnPlayerId { get; set; } // needless to say this is moot if game is over
	}
}
