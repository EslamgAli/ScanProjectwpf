using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace ScanProject.SignalR
{
    public class MessageClient
    {
        private HubConnection _connection;

        // private bool isCompleted = false;

        public void CreateConncetion()
        {

            bool isCompleted = false;
            var _connectionBuilder = new HubConnectionBuilder();
            _connection = _connectionBuilder.WithUrl("http://localhost:5029/chat").WithAutomaticReconnect().Build();

            if (_connection.State == HubConnectionState.Disconnected)
            {
                _connection.On<string>("startscan", (string messageContent) =>
                {
                    DoTwainWork();
                    isCompleted = true;
                });

                _connection.StartAsync().GetAwaiter().GetResult();

                while (!isCompleted)
                {
                    Task.Delay(10).GetAwaiter().GetResult();
                }
            }
        }

        private void DoTwainWork()
        {
            Console.WriteLine("startscan");
        }
    }
}
