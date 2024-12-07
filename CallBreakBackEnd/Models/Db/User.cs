using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CallBreakBackEnd.Models.Db
{
	public class User
	{
		[Key]
		public int Id { get; set; }

		[MaxLength(256)]
		public string Email { get; set; }

		public DateTime LastLoggedInAt { get; set; }

		[MaxLength(40)]
		public string RefreshToken { get; set; }

		[MaxLength(50)]
		public string? FullName { get; set; }

		[MaxLength(20)]
		public string? PetName { get; set; }

		public static User Create(string email, string refreshToken)
		{
			return new User
			{
				Email = email,
				LastLoggedInAt = DateTime.UtcNow,
				RefreshToken = refreshToken
			};
		}
	}
}