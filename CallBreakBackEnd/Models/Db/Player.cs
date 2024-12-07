using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CallBreakBackEnd.Models.Db
{
	public class Player
	{
		[Key]
		public int Id { get; set; }

		public Guid Uid { get; set; }

		[ForeignKey(nameof(Room))]
		public int RoomId { get; set; }

		[MaxLength(50)]
		public string Name { get; set; }

		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public DateTime JoinedAt { get; set; }

		// a player may or may not be a registered user, hence the nullable type
		public int? UserId { get; set; }

		public Room Room { get; set; }

		public static Player Create(string name, int roomId)
		{
			return new Player
			{
				Name = name,
				RoomId = roomId,
				Uid = Guid.NewGuid(),
			};
		}
	}
}
