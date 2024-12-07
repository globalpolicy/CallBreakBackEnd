namespace CallBreakBackEnd.Services.Exceptions
{
	public class WinnerNotAvailableForRoundException : Exception
	{
		private const string _message = "The Winner column is null for given round!";
		public WinnerNotAvailableForRoundException(string? message = _message) : base(message)
		{
		}
	}
}
