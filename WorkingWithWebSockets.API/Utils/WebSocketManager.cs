using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WorkingWithWebSockets.API.Utils
{
    public class WebSocketManager
    {
        private readonly IDictionary<string, WebSocket> connections;

        public WebSocketManager()
        {
            connections = new Dictionary<string, WebSocket>();
        }

        public async Task Register(string user, WebSocket websocket)
        {
            WebSocket currentWebSocket;
            if (connections.TryGetValue(user, out currentWebSocket))
            {
                if (currentWebSocket.State == WebSocketState.CloseReceived) {
                    await currentWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Existing connection", CancellationToken.None)
                    .ContinueWith(a => currentWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Existing connection", CancellationToken.None))
                    .ContinueWith(a => Console.Write("closed connection"));
                    connections.Remove(user);
                }
                else
                {
                    var userConnection = connections[user];
                    System.Threading.CancellationTokenSource cancelationTokenSource = new System.Threading.CancellationTokenSource();
                    ArraySegment<byte> buffer = new ArraySegment<byte>(System.Text.Encoding.ASCII.GetBytes($"Ooops!This connection is already open under ID {userConnection.GetHashCode()} and its status is {userConnection.State}".ToCharArray()));
                    await userConnection.SendAsync(buffer, WebSocketMessageType.Text, true, cancelationTokenSource.Token).ContinueWith(task => Console.WriteLine("Message Sent"));
                }
            }
            else
            {
                connections.Add(user, websocket);
                await StartListening(user, websocket);
            }
        }

        public void CloseSocket(string user)
        {
            WebSocket webSocket;
            
            if (connections.TryGetValue(user, out webSocket))
            {
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Terminating by inactivity", CancellationToken.None)
                    .ContinueWith(a => webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Terminating by inactivity", CancellationToken.None))
                    .ContinueWith(a => Console.Write("closed connection"));
                connections.Remove(user);
            }
        }



        private async Task StartListening(string user, WebSocket websocket)
        {
            byte[] buffer = new byte[1024 * 4];
            bool closeChanel = false;
            WebSocketReceiveResult result = null;
            while (!closeChanel)
            {
                result = await websocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (!(closeChanel = result.CloseStatus.HasValue)) {
                    HandleRequest(buffer, user, websocket);
                }
            }
        }

        private void HandleRequest(byte[] buffer,string user, WebSocket websocket)
        {
            Console.WriteLine(buffer);
            var command = Encoding.ASCII.GetString(buffer);
            if (command == "CLOSE") {
                CloseSocket(user);
            }
            SendMessage(websocket, user).ContinueWith(t => SendMessage(websocket, command)).ContinueWith(t => Console.WriteLine("Message Sent"));  
        }

        public async Task SendMessage(string userSocket, Func<string> getMessage)
        {
            WebSocket webSocket;
            if (connections.TryGetValue(userSocket, out webSocket) && webSocket.State == WebSocketState.Open)
            {
                await SendMessage(connections[userSocket], getMessage());
            }
        }
        public async Task SendMessage(WebSocket socket, string message)
        {
            System.Threading.CancellationTokenSource cancelationTokenSource = new System.Threading.CancellationTokenSource();
            ArraySegment<byte> buffer = new ArraySegment<byte>(System.Text.Encoding.ASCII.GetBytes(message.ToCharArray()));

            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancelationTokenSource.Token);
        }

        public void BroadcastMessage(string message)
        {
            Parallel.ForEach(connections, entry =>
            {
                Task.WaitAll(SendMessage(entry.Value, message));
            });
        }
    }
}
