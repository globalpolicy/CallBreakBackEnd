using System.ComponentModel.DataAnnotations;

namespace CallBreakBackEnd.Models.DTO.Input
{
	public struct EditProfileInfo
	{
		[MaxLength(50)]
		public string FullName { get; set; }

		[MaxLength(20)]
		public string UserName { get; set; }
	}
}
