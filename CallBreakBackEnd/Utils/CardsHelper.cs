using CallBreakBackEnd.Utils.CardTypes;

namespace CallBreakBackEnd.Utils
{
	public class CardsHelper
	{
		private static Card[] lowlyCards = {
			Card.FromString("2D"),
			Card.FromString("2H"),
			Card.FromString("2C"),
			Card.FromString("3D"),
			Card.FromString("3H"),
			Card.FromString("3C"),
		};

		/// <summary>
		/// Returns a list of array of Card, each element of the list representing the hand of cards dealt to the player
		/// </summary>
		/// <param name="numberOfPlayers">The number of hands to generate.</param>
		/// <returns></returns>
		public static List<Card[]> GenerateHandsOfCards(int numberOfPlayers)
		{
			List<Card[]> retval = new List<Card[]>();

			int noOfCardsToExclude = GetNumberOfCardsToBeRemoved(numberOfPlayers);

			// get a new deck with the right number of lowly cards removed
			Card[] deckCards = Deck.Cards.Except(lowlyCards.Take(noOfCardsToExclude)).ToArray();

			// shuffle the deck
			Random.Shared.Shuffle(deckCards);

			// calculate the size of partition
			int handSize = deckCards.Length / numberOfPlayers;

			// perform partitioning
			for (int i = 0; i < numberOfPlayers; i++)
			{
				Card[] hand = deckCards.Skip(i * handSize).Take(handSize).ToArray();
				retval.Add(hand);
			}

			return retval;
		}

		/// <summary>
		/// Generate a list of hands, each represented by a string containing comma-delimited strings of cards.
		/// </summary>
		/// <param name="numberOfPlayers">Total number of hands to generate.</param>
		/// <returns></returns>
		public static List<string> GenerateHandsOfCardsAsStrings(int numberOfPlayers)
		{
			List<Card[]> hands = GenerateHandsOfCards(numberOfPlayers);

			return hands.Select(hand => string.Join(',', hand.Select(card => card.ToString()))).ToList();
		}

		/// <summary>
		/// Returns the winning playerId from given list of (playerId, playedCard) tuples
		/// </summary>
		/// <param name="playerIdCardList">A list of tuple(playerId, playedCard). Note: This list should be in play order.</param>
		/// <returns></returns>
		public static int CalculateWinnerFromPlayedCards(List<Tuple<int, string>> playerIdCardList)
		{
			int retval = 0;

			// transform the given card strings to Cards
			Tuple<int, Card>[] playerIdCards = playerIdCardList.Select(tuple => new Tuple<int, Card>(tuple.Item1, Card.FromString(tuple.Item2))).ToArray();

			// get spades cards if any
			Tuple<int, Card>[] spades = playerIdCards.Where(tuple => tuple.Item2.Suit == Suit.Spades).ToArray();

			if (spades.Length > 0)
			{
				// we're in luck. the winner is the largest card here
				retval = spades.MaxBy(tuple => tuple.Item2.Magnitude)!.Item1;
			}
			else
			{
				// no spades at all

				// assume the leading (first) card is the winner
				retval = playerIdCards.First().Item1;

				// get all cards of the same suit as the leading card
				Tuple<int, Card>[] relevantCards = playerIdCards.Where(tuple => tuple.Item2.Suit == playerIdCards.First().Item2.Suit).ToArray();
				if (relevantCards.Length > 1)
				{
					// there are other cards of the leading suit. the max magnitude of them is the winner
					retval = relevantCards.MaxBy(tuple => tuple.Item2.Magnitude)!.Item1;
				}
			}

			return retval;
		}

		/// <summary>
		/// Retrieves the number of cards to exclude from a full deck of 52 for the given number of players.
		/// </summary>
		/// <param name="noOfPlayers"></param>
		/// <returns></returns>
		/// <exception cref="WrongNumberOfPlayersException"></exception>
		public static int GetNumberOfCardsToBeRemoved(int noOfPlayers)
		{
			return noOfPlayers switch
			{
				2 => 0,
				3 => 1,
				4 => 0,
				5 => 2,
				6 => 4,
				_ => throw new WrongNumberOfPlayersException($"Cannot have a game with {noOfPlayers} players.")
			};
		}

		/// <summary>
		/// Retrieves the number of cards in the deck used to play for the given number of players.
		/// </summary>
		/// <param name="noOfPlayers"></param>
		/// <returns></returns>
		public static int GetDeckSize(int noOfPlayers) => 52 - GetNumberOfCardsToBeRemoved(noOfPlayers);

	}
}
