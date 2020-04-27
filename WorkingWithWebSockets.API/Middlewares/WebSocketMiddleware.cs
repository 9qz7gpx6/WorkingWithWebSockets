using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using WorkingWithWebSockets.API.Utils;

namespace WorkingWithWebSockets.API.Middlewares
{
    public class WebSocketMiddleware : IMiddleware
    {
        private readonly Utils.WebSocketManager webSocketManager;

        public WebSocketMiddleware(Utils.WebSocketManager webSocketManager)
        {
            this.webSocketManager = webSocketManager;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Path == "/ws")
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    
                    await webSocketManager.Register(GetUser(context), webSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            }
            else
            {
                await next(context);
            }
        }

        private string GetUser(HttpContext context)
        {
            //Implement here the strategy for resolve the connection owner name
            return context.Request.Query["user"];
        }
    }
}
