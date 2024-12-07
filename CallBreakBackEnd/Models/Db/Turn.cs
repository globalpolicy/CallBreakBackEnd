using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CallBreakBackEnd.Models.Db
{
	public class Turn
	{
		[Key]
		public int Id { get; set; }

		[ForeignKey(nameof(Round))]
		public int RoundId { get; set; }

		[ForeignKey(nameof(Player))]
		public int PlayerId { get; set; }

		[MaxLength(256)]
		public string HandCards { get; set; }

		[MaxLength(3)]
		public string? PlayedCard { get; set; } // is empty for a turn belonging to the first (card dealing) round

		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public DateTime CreatedAt { get; set; }
		public Round Round { get; set; }
		public Player Player { get; set; }

		public static Turn Create(int roundId, int playerId, string handCards, string playedCard)
		{
			return new Turn
			{
				RoundId = roundId,
				PlayerId = playerId,
				HandCards = handCards,
				PlayedCard = playedCard
			};
		}
	}
}
