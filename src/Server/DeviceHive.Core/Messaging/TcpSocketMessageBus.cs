using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceHive.Core.Messaging
{
    /// <summary>
    /// Represents message bus implementation using TCP sockets
    /// </summary>
    public class TcpSocketMessageBus : MessageBus, IDisposable
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(TcpSocketMessageBus));
        private readonly TcpSocketMessageBusConfiguration _configuration;
        private readonly TaskCompletionSource<object> _cancelTaskCompletionSource;
        private readonly Task _listenTask;
        private readonly ClientConnection[] _clientConnections;

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TcpSocketMessageBus()
        {
            _configuration = (TcpSocketMessageBusConfiguration)ConfigurationManager.GetSection("tcpSocketMessageBus");
            if (_configuration == null)
                throw new InvalidOperationException("tcpSocketMessageBus configuration section cannot be found!");

            _cancelTaskCompletionSource = new TaskCompletionSource<object>();

            _listenTask = Task.Run(async () => await ListenAsync());
            _clientConnections = _configuration.ClientEndpoints.Cast<TcpSocketEndpointElement>().Select(e => new ClientConnection(e)).ToArray();
        }
        #endregion

        #region MessageBus Members

        /// <summary>
        /// Sends message to all remote listeners
        /// </summary>
        /// <param name="data">Message data</param>
        protected override void SendMessage(byte[] data)
        {
            foreach (var clientConnection in _clientConnections)
            {
                clientConnection.TrySend(data);
            }
        }
        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes the current object
        /// </summary>
        public void Dispose()
        {
            // close client connections
            foreach (var clientConnection in _clientConnections)
                clientConnection.Close();

            // close server connection
            _cancelTaskCompletionSource.SetCanceled();
            _listenTask.Wait();
        }
        #endregion

        #region Private Methods

        private async Task ListenAsync()
        {
            var listener = new TcpListener(IPAddress.Any, _configuration.ServerPort);
            listener.Start();
            _log.InfoFormat("TCP listener started on port {0}", _configuration.ServerPort);

            try
            {
                while (true)
                {
                    var acceptTask = listener.AcceptSocketAsync();
                    if (await Task.WhenAny(acceptTask, _cancelTaskCompletionSource.Task) != acceptTask)
                        return;

                    var socket = acceptTask.Result;
                    _log.InfoFormat("Accepted connection from: {0}", socket.RemoteEndPoint);
                    var task = Task.Run(async () => await ReceiveAsync(socket));
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error while listening for TCP clients", ex);
            }
            finally
            {
                listener.Stop();
                _log.Info("TCP listener stopped");
            }
        }

        private async Task ReceiveAsync(Socket socket)
        {
            try
            {
                while (true)
                {
                    var receiveLengthTask = ReceiveDataAsync(socket, 4);
                    if (await Task.WhenAny(receiveLengthTask, _cancelTaskCompletionSource.Task) != receiveLengthTask)
                        return; // bus is disposed

                    if (receiveLengthTask.Result == null)
                        return; // connection closed by client

                    var dataLength = BitConverter.ToInt32(receiveLengthTask.Result, 0);

                    var receiveDataTask = ReceiveDataAsync(socket, dataLength);
                    if (await Task.WhenAny(receiveDataTask, _cancelTaskCompletionSource.Task) != receiveDataTask)
                        return; // bus is disposed

                    if (receiveDataTask.Result == null)
                        return; // connection closed by client

                    HandleMessage(receiveDataTask.Result);
                }
            }
            catch (Exception ex)
            {
                _log.Warn(string.Format("Error while retrieving data from TCP socket: {0}", socket.RemoteEndPoint), ex);
            }
            finally
            {
                _log.InfoFormat("Closing server connection to: {0}", socket.RemoteEndPoint);
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }

        private async Task<byte[]> ReceiveDataAsync(Socket socket, int length)
        {
            var received = 0;
            var buffer = new byte[length];

            while (true)
            {
                var result = socket.BeginReceive(buffer, received, length - received, SocketFlags.None, null, null);
                var bytes = await Task<int>.Factory.FromAsync(result, socket.EndReceive);
                if (bytes == 0)
                    return null; // client closed connection

                received += bytes;
                if (received == length)
                    return buffer; // successfullly received data
            }
        }
        #endregion

        #region ClientConnection class

        private class ClientConnection
        {
            private const int RetryTimeout = 5000; // in milliseconds

            private readonly object _lock = new object();
            private readonly ILog _log = LogManager.GetLogger(typeof(TcpSocketMessageBus));
            private readonly TcpSocketEndpointElement _endpoint;
            private readonly TaskCompletionSource<object> _cancelTaskCompletionSource;
            private readonly Task _connectTask;

            private TcpClient _client;

            public ClientConnection(TcpSocketEndpointElement endpoint)
            {
                _endpoint = endpoint;
                _cancelTaskCompletionSource = new TaskCompletionSource<object>();
                _connectTask = Task.Run(async () => await ConnectAsync());
            }

            public bool TrySend(byte[] data)
            {
                if (_client == null || !_client.Connected)
                    return false; // endpoint is not available

                lock (_lock)
                {
                    try
                    {
                        var stream = _client.GetStream();
                        var buffer = BitConverter.GetBytes(data.Length);
                        stream.Write(buffer, 0, buffer.Length);
                        stream.Write(data, 0, data.Length);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(string.Format("Error while sending data to TCP socket: {0}", _client.Client.RemoteEndPoint), ex);
                        return false;
                    }
                }
            }

            public void Close()
            {
                _cancelTaskCompletionSource.SetCanceled();
                _connectTask.Wait();
            }

            private async Task ConnectAsync()
            {
                while (true)
                {
                    // create or restore connection
                    try
                    {
                        lock (_lock)
                        {
                            if (_client != null && !_client.Connected)
                            {
                                // if socked is broken - close it
                                _log.InfoFormat("Closing client connection to: {0}", _client.Client.RemoteEndPoint);
                                _client.Close();
                                _client = null;
                            }

                            if (_client == null)
                            {
                                // try to connect
                                _client = new TcpClient(_endpoint.Host, _endpoint.Port);
                                _log.InfoFormat("Opened client connection to: {0}", _client.Client.RemoteEndPoint);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(string.Format("Error while opening client connection to: {0}:{1}", _endpoint.Host, _endpoint.Port), ex);
                    }
                    
                    // wait for retry timeout
                    var delayTask = Task.Delay(RetryTimeout);
                    if (await Task.WhenAny(delayTask, _cancelTaskCompletionSource.Task) != delayTask)
                    {
                        // close connection
                        lock (_lock)
                        {
                            if (_client != null)
                            {
                                _log.InfoFormat("Closing client connection to: {0}", _client.Client.RemoteEndPoint);
                                _client.Close();
                                _client = null;
                            }
                        }
                        return;
                    }
                }
            }
        }
        #endregion
    }
}
