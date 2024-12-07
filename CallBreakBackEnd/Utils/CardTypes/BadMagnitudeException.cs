namespace CallBreakBackEnd.Utils.CardTypes
{
	public class BadMagnitudeException : Exception
	{
		private const string _message = "The given card magnitude is illegal.";
		public BadMagnitudeException(string? message = _message) : base(message)
		{
		}
	}
}
