namespace CallBreakBackEnd.Models.DTO.Output
{
	public class ScoreInfo
	{
		public int PlayerId { get; set; }
		public string PlayerName { get; set; }
		public int DeclaredScore { get; set; }
		public int? ActualScore { get; set; }
	}
}
