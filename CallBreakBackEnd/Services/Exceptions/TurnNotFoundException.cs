namespace CallBreakBackEnd.Services.Exceptions
{
	public class TurnNotFoundException : Exception
	{
		private const string _message = "No Turn entry found!";
		public TurnNotFoundException(string? message = _message) : base(message)
		{
		}
	}
}
