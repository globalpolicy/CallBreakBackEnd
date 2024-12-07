namespace CallBreakBackEnd.Services.Exceptions
{
	public class OutOfTurnPlayException : Exception
	{
		private const string _message = "Attempt to play out of turn!";
		public OutOfTurnPlayException(string? message = _message) : base(message)
		{
		}
	}
}
