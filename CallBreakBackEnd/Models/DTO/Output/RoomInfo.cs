using CallBreakBackEnd.Models.Db;

namespace CallBreakBackEnd.Models.DTO.Output
{
	public class RoomInfo
	{
		public int RoomId { get; set; }
		public Guid RoomUid { get; set; }
		public int Capacity { get; set; }
		public DateTime CreatedAt { get; set; }
		public bool IsActive { get; set; }
		public Guid? RoomAdminUid { get; set; }

	}
}
