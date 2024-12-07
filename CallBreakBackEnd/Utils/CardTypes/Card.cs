namespace CallBreakBackEnd.Utils.CardTypes
{
	public enum Suit
	{
		Spades,
		Clubs,
		Diamonds,
		Hearts
	}

	public struct Card
	{
		public int Magnitude { get; set; }
		public Suit Suit { get; set; }

		public override bool Equals(object? obj)
		{
			bool retval = false;
			if (obj is Card otherCard)
			{
				retval = Magnitude == otherCard.Magnitude && Suit == otherCard.Suit;
			}
			return retval;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Magnitude, Suit);
		}

		public override string ToString()
		{
			return $"{StringFromMagnitude(Magnitude)}{CharFromSuit(Suit)}";
		}

		/// <summary>
		/// Creates a Card object from its string representation. Eg. 10S => 10 of Spades, AD => 100 of Diamonds (Ace has a magnitude of 100), JC => Joker of Clubs, etc.
		/// </summary>
		/// <param name="card"></param>
		/// <returns></returns>
		public static Card FromString(string card)
		{
			if (card.Length != 2 && card.Length != 3)
				throw new BadCardStringException();

			char suitChar = card.Last();
			string magnitudeString = card.Substring(0, card.Length - 1);

			return new Card
			{
				Magnitude = MagnitudeFromString(magnitudeString),
				Suit = SuitFromChar(suitChar)
			};
		}

		/// <summary>
		/// Converts the given card magnitude string to the correct integer value, e.g. "A"=>100, "J"=>11, "Q"=>12, "K"=>13, "2"=>2, "3"=>3, and so on.
		/// </summary>
		/// <param name="magnitudeString"></param>
		/// <returns></returns>
		/// <exception cref="BadCardStringException"></exception>
		private static int MagnitudeFromString(string magnitudeString)
		{
			switch (magnitudeString.ToUpper())
			{
				case "A":
					return 100;
				case "J":
					return 11;
				case "Q":
					return 12;
				case "K":
					return 13;
				default:
					if (int.TryParse(magnitudeString, out int magnitude))
						return magnitude;
					else
						throw new BadCardStringException("Invalid magnitude string.");
			}
		}

		/// <summary>
		/// Converts the given card magnitude to the correct string representation.
		/// </summary>
		/// <param name="magnitude"></param>
		/// <returns></returns>
		/// <exception cref="BadMagnitudeException"></exception>
		private static string StringFromMagnitude(int magnitude)
		{
			switch (magnitude)
			{
				case >= 2 and <= 10:
					return magnitude.ToString();
				case 100:
					return "A";
				case 11:
					return "J";
				case 12:
					return "Q";
				case 13:
					return "K";
				default:
					throw new BadMagnitudeException();
			}
		}

		/// <summary>
		/// Converts the given suit character to a Suit enum instance
		/// </summary>
		/// <param name="suitChar"></param>
		/// <returns></returns>
		/// <exception cref="BadCardStringException"></exception>
		private static Suit SuitFromChar(char suitChar)
		{
			switch (char.ToUpper(suitChar))
			{
				case 'S':
					return Suit.Spades;
				case 'C':
					return Suit.Clubs;
				case 'D':
					return Suit.Diamonds;
				case 'H':
					return Suit.Hearts;
				default:
					throw new BadCardStringException("Invalid suit name.");
			}
		}

		/// <summary>
		/// Converts the given Suit to the correct char representation.
		/// </summary>
		/// <param name="suit"></param>
		/// <returns></returns>
		/// <exception cref="BadSuitException"></exception>
		private static char CharFromSuit(Suit suit)
		{
			switch (suit)
			{
				case Suit.Spades:
					return 'S';
				case Suit.Clubs:
					return 'C';
				case Suit.Diamonds:
					return 'D';
				case Suit.Hearts:
					return 'H';
				default:
					throw new BadSuitException();
			}
		}


	}
}