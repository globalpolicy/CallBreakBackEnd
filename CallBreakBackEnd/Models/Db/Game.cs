using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CallBreakBackEnd.Models.Db
{
	public class Game
	{
		[Key]
		public int Id { get; set; }

		public bool IsActive { get; set; }
		public bool IsFinished { get; set; }

		[ForeignKey(nameof(Room))]
		public int RoomId { get; set; }

		[ForeignKey(nameof(DealerPlayer))]
		public int DealerPlayerId { get; set; }

		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public DateTime CreationDate { get; set; }

		public Player DealerPlayer { get; set; }

		public Room Room { get; set; }

		public ICollection<Round> Rounds { get; set; } = new List<Round>();

		public static Game Create(int roomId, int dealerPlayerId)
		{
			return new Game
			{
				RoomId = roomId,
				IsActive = true,
				DealerPlayerId = dealerPlayerId
			};
		}
	}
}
