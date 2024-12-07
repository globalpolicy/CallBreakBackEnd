namespace CallBreakBackEnd.Utils.CardTypes
{
	public class BadSuitException : Exception
	{
		private const string _message = "The given suit is illegal.";
		public BadSuitException(string? message = _message) : base(message)
		{
		}

	}
}
