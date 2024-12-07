namespace CallBreakBackEnd.Services.Exceptions
{
	public class RoundNotFoundException : Exception
	{
		private const string _message = "Round not found!";
		public RoundNotFoundException(string? message = _message) : base(message)
		{
		}
	}
}
