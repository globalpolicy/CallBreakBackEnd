using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CallBreakBackEnd.Models.Db
{
	public class Score
	{
		[Key]
		public int Id { get; set; }

		[ForeignKey(nameof(Player))]
		public int PlayerId { get; set; }

		[ForeignKey(nameof(Game))]
		public int GameId { get; set; }

		public int DeclaredScore { get; set; }
		public int ActualScore { get; set; } // gradually incremented at the end of each round if PlayerId is the winner for the round

		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public DateTime CreatedAt { get; set; }
		public Player Player { get; set; }
		public Game Game { get; set; }

		public static Score Create(int playerId, int gameId, int declaredScore)
		{
			return new Score
			{
				PlayerId = playerId,
				GameId = gameId,
				DeclaredScore = declaredScore
			};
		}
	}
}
