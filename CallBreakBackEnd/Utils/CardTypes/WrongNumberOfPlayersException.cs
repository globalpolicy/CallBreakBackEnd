namespace CallBreakBackEnd.Utils.CardTypes
{
	public class WrongNumberOfPlayersException : Exception
	{
		private const string _message = "Illegal player count.";
		public WrongNumberOfPlayersException(string? message = _message) : base(message)
		{
		}
	}
}
