namespace CallBreakBackEnd.Utils.CardTypes
{
	public static class Deck
	{
		// static private field
		private static readonly Card[] _fullDeck;

		// static public property
		/// <summary>
		/// Get a copy of the full deck of 52 cards
		/// </summary>
		public static List<Card> Cards
		{
			get
			{
				return _fullDeck.ToList(); // hand a copy of the full deck to any caller
			}
		}

		// static constructor. only executed once in the application's lifetime. only responsibility is to populate _fullDeck
		static Deck()
		{
			List<Card> deck = new List<Card>();

			string[] magnitudes = new[] { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
			char[] suits = new[] { 'S', 'C', 'D', 'H' };
			foreach (string magnitudeStr in magnitudes)
			{
				foreach (char suitChar in suits)
				{
					string cardStr = string.Concat(magnitudeStr, suitChar);
					deck.Add(Card.FromString(cardStr));
				}
			}

			_fullDeck = deck.ToArray();
		}

	}
}
