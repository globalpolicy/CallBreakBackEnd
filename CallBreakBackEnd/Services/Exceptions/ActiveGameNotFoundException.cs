namespace CallBreakBackEnd.Services.Exceptions
{
	public class ActiveGameNotFoundException : Exception
	{
		private const string _message = "No active game found for the player's room.";
		public ActiveGameNotFoundException(string? message = _message) : base(message)
		{
		}
	}
}
