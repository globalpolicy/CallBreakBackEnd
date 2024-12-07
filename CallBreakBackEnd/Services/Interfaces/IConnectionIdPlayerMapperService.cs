namespace CallBreakBackEnd.Services.Interfaces
{
	public interface IConnectionIdPlayerMapperService
	{
		public void AddPlayer(string connectionId, string playerUid);
		public void RemovePlayer(string connectionId);
		public string GetPlayerUid(string connectionId);
		public bool IsPlayerOnline(string playerUid);
		public void RemoveClient(string connectionId);
		public int NumberOfConnectionsForPlayer(string playerUid);
	}
}
