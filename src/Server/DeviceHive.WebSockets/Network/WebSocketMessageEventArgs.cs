namespace DeviceHive.WebSockets.Network
{
    public class WebSocketMessageEventArgs : WebSocketConnectionEventArgs
	{
		public WebSocketMessageEventArgs(WebSocketConnectionBase connection, string message) :
            base(connection)
		{
			Message = message;
		}

		public string Message { get; private set; }
	}
}
