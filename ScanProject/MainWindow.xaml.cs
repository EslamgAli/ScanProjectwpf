using Microsoft.AspNetCore.SignalR.Client;
using ScanProject.ViewModel;
using System;
using System.Windows;
using System.Windows.Interop;

namespace ScanProject
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;
        #region signalr
        private HubConnection _connection;
        private HubConnectionBuilder _connectionBuilder;
        bool isCompleted = false;
        #endregion
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            _viewModel = (MainWindowViewModel)DataContext;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _viewModel.WindowHandle = new WindowInteropHelper(this).Handle;

            /*  _connectionBuilder = new HubConnectionBuilder();
              _connection = _connectionBuilder.WithUrl("http://localhost:5029/chat").WithAutomaticReconnect().Build();

              if (_connection.State == HubConnectionState.Disconnected)
              {
                  _connection.On<string>("startscan", (string messageContent) =>
                  {

                      visible();
                      isCompleted = true;
                  });

                  _connection.StartAsync().GetAwaiter().GetResult();

                  while (!isCompleted)
                  {
                      Task.Delay(10).GetAwaiter().GetResult();
                  }
              }*/


            // _viewModel.WindowHandle = new WindowInteropHelper(this).Handle;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
