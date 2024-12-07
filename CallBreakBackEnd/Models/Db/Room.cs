using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace CallBreakBackEnd.Models.Db
{
	public class Room
	{
		[Key]
		public int Id { get; set; }

		// a room must have one admin user
		[ForeignKey(nameof(AdminUser))]
		public int AdminUserId { get; set; }

		public Guid Uid { get; set; }

		public int Capacity { get; set; }
		public bool Active { get; set; }

		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public DateTime CreatedAt { get; set; }

		public ICollection<Player> Players { get; } = new List<Player>();

		public ICollection<Game> Games { get; } = new List<Game>();
		public User AdminUser { get; set; }

		public static Room Create(int adminUserId, int capacity)
		{
			return new Room
			{
				Uid = Guid.NewGuid(),
				AdminUserId = adminUserId,
				Capacity = capacity,
				Active = true,
			};
		}
	}
}
