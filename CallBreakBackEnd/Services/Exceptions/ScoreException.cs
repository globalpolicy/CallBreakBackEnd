namespace CallBreakBackEnd.Services.Exceptions
{
	public class ScoreException : Exception
	{
		private const string _message = "Players have not finished declaring their anticipated scores for the game!";
		public ScoreException(string? message = _message) : base(message)
		{
		}
	}
}
