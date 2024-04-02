using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.AspNetCore.SignalR.Client;
using NTwain;
using NTwain.Data;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScanProject.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region Private Fields

        private IntPtr _windowHanle;
        private DataSourceVM _selectedDataSources;
        private ICommand _captureCommand;
        private string _capturedImage;
        private TwainCore _twainCore;
        int count = 1;
        #endregion Private Fields

        #region signalr
        public HubConnection _connection;
        private HubConnectionBuilder _connectionBuilder;
        bool isCompleted = false;
        #endregion
        #region Public Fields

        public IntPtr WindowHandle
        {
            get => _windowHanle;
            set
            {
                _windowHanle = value;
                DataSources = _twainCore.GetDataSources();
                RaisePropertyChanged(() => DataSources);
            }
        }

        public int State { get { return _twainCore.State; } }

        public ICommand CaptureCommand
        {
            get
            {
                return _captureCommand ?? (_captureCommand = new RelayCommand(async () =>
                {
                    CapturedImage = await _twainCore.ScanDocumentAsync(@"E:\\", $"docTest{count++}", WindowHandle);
                    RaisePropertyChanged(() => CapturedImage);
                }));
            }
        }

        public ObservableCollection<DataSourceVM> DataSources { get; set; }

        public string CapturedImage
        {
            get => _capturedImage;
            set
            {
                _capturedImage = value;
                RaisePropertyChanged(() => CapturedImage);
                RaisePropertyChanged(() => InfoVisibility);
            }
        }

        public DataSourceVM SelectedDataSources
        {
            get => _selectedDataSources;
            set
            {
                if (_twainCore.State == 4)
                    _twainCore.CurrentSource.Close();

                _selectedDataSources = value;
                _selectedDataSources?.Open();
                //   _selectedDataSources?.DS.Capabilities.ICapXferMech.SetValue(XferMech.File);
                _selectedDataSources?.DS.Capabilities.ICapXferMech.SetValue(XferMech.Native);
                RaisePropertyChanged(() => SelectedDataSources);
                RaisePropertyChanged(() => CaptureCommand);
                MessageBox.Show("Data Source Opened !");
            }
        }

        public Visibility InfoVisibility
        {
            get => CapturedImage == null ? Visibility.Visible : Visibility.Hidden;
        }

        #endregion Public Fields

        #region Constructor

        public MainWindowViewModel()
        {
            int count = 0;
            //Open by default the second DataSource
            _twainCore = new TwainCore(1);
            _twainCore.StateChanged += (sender, e) =>
            {
                RaisePropertyChanged(() => State);

                if (State == 4 && count != 0)
                {
                    count = 0;

                    _connection.SendAsync("ScanCompleted", _twainCore.folderName).GetAwaiter();
                }

                count++;
            };

            _twainCore.mainWindowViewModel = this;
            // MessageClient messageClient = new MessageClient();
            //  messageClient.CreateConncetion();
            //signalr

            _connectionBuilder = new HubConnectionBuilder();
            _connection = _connectionBuilder.WithUrl("http://localhost:5029/chat").WithAutomaticReconnect().Build();

            // if (_connection.State == HubConnectionState.Disconnected)
            // {
            _connection.On<string>("startscan", async (string messageContent) =>
            {
                _twainCore.folderName = messageContent;
                // DoTwainWork();
                //_twainCore.StartSource(WindowHandle);
                await _twainCore.ScanDocumentAsync(@"E:\\", $"scanned_image_{count++}", WindowHandle);
                //RaisePropertyChanged(() => CaptureCommand);
                isCompleted = true;

            });

            _connection.StartAsync().GetAwaiter().GetResult();

            /* while (!isCompleted)
             {
                 Task.Delay(10).GetAwaiter().GetResult();
             }*/
            //}
        }

        #endregion Constructor

        #region Helpers Methods

        /// <summary>
        /// Generate final from scanner device scan data
        /// </summary>
        /// <param name="e"><see cref="DataTransferredEventArgs"/> all informatione about scan data </param>
        /// <returns></returns>
        private ImageSource GenerateImage(DataTransferredEventArgs e)
        {
            BitmapSource img = null;

            switch (e.TransferType)
            {
                case XferMech.Native:
                    using (var stream = e.GetNativeImageStream())
                    {
                        if (stream != null)
                        {
                            img = stream.ConvertToWpfBitmap(720, 0);
                        }
                    }

                    break;

                case XferMech.File:
                    img = new BitmapImage(new Uri(e.FileDataPath));
                    if (img.CanFreeze)
                    {
                        img.Freeze();
                    }

                    break;

                case XferMech.Memory:
                    break;
            }

            return img;
        }

        /// <summary>
        /// Generate unique name for the new file
        /// </summary>
        /// <param name="dir">Directory where the new file will be saved</param>
        /// <param name="name">The name of the final file </param>
        /// <param name="ext">File extension, in this demo <see cref="FileFormat.Bmp"/> or <see cref="FileFormat.Tiff"/></param>
        /// <returns></returns>
        private string GetUniqueName(string dir, string name, string ext)
        {
            var filePath = Path.Combine(dir, name + ext);
            int next = 1;
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(dir, string.Format("{0} ({1}){2}", name, next++, ext));
            }

            return filePath;
        }

        #endregion Helpers Methods
    }
}