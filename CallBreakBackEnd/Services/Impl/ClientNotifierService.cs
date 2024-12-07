using CallBreakBackEnd.Hubs;
using CallBreakBackEnd.Models.DTO.Output;
using CallBreakBackEnd.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CallBreakBackEnd.Services.Impl
{
	/// <summary>
	/// This class is responsible for delivering notifications to connected clients.
	/// </summary>
	public class ClientNotifierService : IClientNotifierService
	{
		private IHubContext<GameHub, IGameClient> _hubContext;
		public ClientNotifierService(IHubContext<GameHub, IGameClient> hubContext)
		{
			_hubContext = hubContext;
		}

		public void NotifyCardsDealt(string roomUid)
		{
			_hubContext.Clients.Group(roomUid).CardsHaveBeenDealt();
		}

		public void NotifyScoresUpdated(string roomUid)
		{
			_hubContext.Clients.Group(roomUid).ScoresUpdated();
		}

		public void NotifyCardPlayed(string roomUid, TurnInfoShort recentTurnOutcomeInfo)
		{
			_hubContext.Clients.Group(roomUid).CardHasBeenPlayed(recentTurnOutcomeInfo);
		}
	}
}
