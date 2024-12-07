namespace CallBreakBackEnd.Utils
{
	public static class Randoms
	{
		private static readonly Random _random;
		static Randoms()
		{
			_random = new Random();
		}

		public static T GetRandomElement<T>(ICollection<T> collection)
		{
			int randomIndex = _random.Next(collection.Count);
			return collection.ElementAt(randomIndex);
		}
	}
}
