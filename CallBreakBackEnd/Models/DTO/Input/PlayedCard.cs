using System.ComponentModel.DataAnnotations;

namespace CallBreakBackEnd.Models.DTO.Input
{
	public class PlayedCard
	{
		public string PlayerUid { get; set; }

		[MaxLength(3, ErrorMessage = "Card string must be 3 characters at most.")]
		public string Card { get; set; }
	}
}
