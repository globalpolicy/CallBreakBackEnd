namespace CallBreakBackEnd.Utils.CardTypes
{
	public class BadCardStringException : Exception
	{
		private const string _message = "The given card string is malformed.";
		public BadCardStringException(string? message = _message) : base(message)
		{
		}

	}
}
