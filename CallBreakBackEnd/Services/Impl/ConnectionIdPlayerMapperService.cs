using System.Collections.Concurrent;
using CallBreakBackEnd.Services.Interfaces;

namespace CallBreakBackEnd.Services.Impl
{
	/// <summary>
	/// This singleton service exists for the sole purpose of maintaining a mapping between SignalR connectionIds and playerUids
	/// </summary>
	public class ConnectionIdPlayerMapperService : IConnectionIdPlayerMapperService
	{
		// a realtime dict of SignalR connectionId : player UID
		private ConcurrentDictionary<string, string> _onlinePlayers = new ConcurrentDictionary<string, string>();

		/// <summary>
		/// Adds the given player uid to the dict of online players
		/// </summary>
		/// <param name="connectionId">The connection id of the connection associated with this user</param>
		/// <param name="playerUid">The user's UID</param>
		public void AddPlayer(string connectionId, string playerUid)
		{
			_onlinePlayers[connectionId] = playerUid;
		}

		/// <summary>
		/// Removes the entries corresponding to the given player uid from the dict of online players
		/// </summary>
		/// <param name="playerUid"></param>
		public void RemovePlayer(string playerUid)
		{
			foreach (var key in _onlinePlayers.Keys.ToList())
			{
				if (_onlinePlayers[key] == playerUid)
					_onlinePlayers.TryRemove(key, out _);
			}
		}

		public void RemoveClient(string connectionId)
		{
			_onlinePlayers.TryRemove(connectionId, out _);
		}

		/// <summary>
		/// Gets the player UID for the given connection id. Many connection ids can have the same player UID, but not the other way around; 
		/// hence connectionId->playerUid is uniquely defined.
		/// </summary>
		/// <param name="connectionId"></param>
		/// <returns></returns>
		public string GetPlayerUid(string connectionId)
		{
			if (_onlinePlayers.ContainsKey(connectionId))
				return _onlinePlayers[connectionId];
			else
				return string.Empty;
		}

		public bool IsPlayerOnline(string playerUid)
		{
			return _onlinePlayers.Values.Contains(playerUid);
		}

		public int NumberOfConnectionsForPlayer(string playerUid)
		{
			int retval = 0;
			foreach (var key in _onlinePlayers.Keys.ToList())
			{
				if (_onlinePlayers[key] == playerUid)
					retval++;
			}
			return retval;
		}
	}
}
