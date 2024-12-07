using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CallBreakBackEnd.Models.Db
{
	public class Round
	{
		[Key]
		public int Id { get; set; }

		public int RoundNumber { get; set; }

		[ForeignKey(nameof(Game))]
		public int GameId { get; set; }

		public int? WinnerPlayerId { get; set; }

		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public DateTime CreatedAt { get; set; }

		public Game Game { get; set; }
		public ICollection<Turn> Turns { get; set; } = new List<Turn>();

		public static Round Create(int roundNumber, int gameId)
		{
			return new Round
			{
				GameId = gameId,
				RoundNumber = roundNumber,
			};
		}
	}
}
