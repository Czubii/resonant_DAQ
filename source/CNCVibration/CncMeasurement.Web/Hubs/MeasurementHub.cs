using Microsoft.AspNetCore.SignalR;
using CncMeasurement.Core.Interfaces;
using CncMeasurement.Core.models;
using System.Threading.Channels;

namespace CncMeasurement.Web.Hubs
{
    public class LiveMeasurementHub : Hub<IMeasurementClient>
    {
        public const string LiveViewersGroup = "LiveViewers";
        /// <summary>
        /// The Web UI calls this method when it wants to start watching live data.
        /// </summary>
        public async Task SubscribeToLiveMeasurements()
        {
            // Add the specific WebSocket connection to the broadcast group
            await Groups.AddToGroupAsync(Context.ConnectionId, LiveViewersGroup);

            // Optional: Send a confirmation back to the specific client that just connected
            await Clients.Caller.ReceiveSystemStatus("Subscribed to live data stream.");
        }
        /// <summary>
        /// The Web UI calls this when the user navigates away from the live dashboard.
        /// </summary>
        public async Task UnsubscribeFromLiveMeasurements()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, LiveViewersGroup);
            await Clients.Caller.ReceiveSystemStatus("Unsubscribed from live data stream.");
        }
        /// <summary>
        /// Automatically triggered when a client drops connection (e.g., closes the browser).
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // SignalR AUTOMATICALLY removes disconnected clients from all groups.
            // You do not need to manually call RemoveFromGroupAsync here.
            // However, this is a great place to log sudden disconnects.

            if (exception != null)
            {
                // Log the error: the client disconnected abnormally (network drop, crash)
            }

            await base.OnDisconnectedAsync(exception);
        }

    }

    public class SignalRMeasurementBroadcaster : IMeasurementBroadcaster
    {
        private readonly IHubContext<LiveMeasurementHub, IMeasurementClient> _hubContext;

        public SignalRMeasurementBroadcaster(IHubContext<LiveMeasurementHub, IMeasurementClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastMeasurementAsync(SampleChunk data, CancellationToken ct)
        {
            await _hubContext.Clients.Group(LiveMeasurementHub.LiveViewersGroup).ReceiveMeasurement(data);
        }
    }
}
