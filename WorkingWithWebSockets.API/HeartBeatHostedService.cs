using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WorkingWithWebSockets.API.Utils;

namespace WorkingWithWebSockets.API
{
    internal class HeartBeatHostedService : IHostedService
    {
        private Task _executingTask;
        private ILogger logger;
        private WebSocketManager webSocketManager;
        private readonly CancellationTokenSource cancelationTokenSource = new CancellationTokenSource();

        public HeartBeatHostedService(ILogger<HeartBeatHostedService> logger, WebSocketManager webSocketManager)
        {
            this.logger = logger;
            this.webSocketManager = webSocketManager;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            
            _executingTask = ExecuteAsync(cancelationTokenSource.Token);

            
            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }
            return Task.CompletedTask;
        }

        protected async Task ExecuteAsync(CancellationToken cancelationToken)
        {
            cancelationToken.Register(() => logger.LogDebug($"Starting heart beat"));
            var currentProcess = Process.GetCurrentProcess();
            
            PerformanceCounter PC = new PerformanceCounter();
            PC.CategoryName = "Process";
            PC.CounterName = "Working Set - Private";
            PC.InstanceName = currentProcess.ProcessName;
            while (!cancelationToken.IsCancellationRequested)
            {
                webSocketManager.BroadcastMessage($"Process id:{currentProcess.Id}, Memory Usage: {Convert.ToInt32(PC.NextValue()) / (int)(1024)}");
                await Task.Delay(5000, cancelationToken);
            }

            logger.LogDebug($"Stopping heart beat");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancelationTokenSource.Cancel();
            return Task.CompletedTask;
        }
    }
}