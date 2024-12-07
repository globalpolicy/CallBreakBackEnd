namespace CallBreakBackEnd.Services.Exceptions
{
	public class NotYourCardToPlayException : Exception
	{
		private const string _message = "The given card does not belong to you.";
		public NotYourCardToPlayException(string? message = _message) : base(message)
		{
		}
	}
}
