namespace CallBreakBackEnd.Models.DTO.Output
{
	public class UserDetails
	{
		public string Email { get; set; }
		public string? FullName { get; set; }
		public string? UserName { get; set; }
		public ICollection<RoomInfo> Rooms { get; set; } = new List<RoomInfo>();
	}
}
