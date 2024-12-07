namespace CallBreakBackEnd.Services.Exceptions
{
	public class PlayerNotFoundException : Exception
	{
		private const string _message = "Player not found.";
		public PlayerNotFoundException(string? message = _message) : base(message)
		{
		}
	}
}
