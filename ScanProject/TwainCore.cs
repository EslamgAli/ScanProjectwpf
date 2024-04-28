using NTwain;
using NTwain.Data;
using ScanProject.ImageUpload;
using ScanProject.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ScanProject
{
    public class TwainCore
    {
        #region Private Property

        private int _state;
        private readonly TwainSession _twainSession;
        private ObservableCollection<DataSourceVM> _dataSources
            = new ObservableCollection<DataSourceVM>();

        private int count = 1;
        private int UploadImgCount = 1;
        private string displayfile = "";
        public string folderName = "";
        private bool isComplete;

        public MainWindowViewModel mainWindowViewModel { get; set; }
        #endregion Private Property
        //IntPtr Handle;
        #region Public Property

        public event EventHandler<StateChangedArgs> StateChanged;
        public event EventHandler SourceDisabled;

        public int State
        {
            get => _state = _twainSession.State;
            private set => _state = value;
        }

        public ObservableCollection<DataSourceVM> GetDataSources()
        {
            //Clean DataSources
            _dataSources.Clear();

            foreach (var s in _twainSession.Select(s => new DataSourceVM { DS = s }))
            {
                _dataSources.Add(s);
            }

            return _dataSources;
        }

        public DataSource CurrentSource { get => _twainSession.CurrentSource; }

        #endregion Public Property

        #region Constructor

        public TwainCore()
        {
            PlatformInfo.Current.PreferNewDSM = false;

            var appId = TWIdentity.CreateFromAssembly(DataGroups.Image, Assembly.GetExecutingAssembly());
            _twainSession = new TwainSession(appId);

            PlatformInfo.Current.PreferNewDSM = false;

            _twainSession.TransferReady += _twainSession_TransferReady;
            //_twainSession.StateChanged += _twainSession_StateChanged;
            _twainSession.SourceDisabled += _twainSession_SourceDisabled;
            DataTransferred();

            if (_twainSession.Open() != ReturnCode.Success)
                throw new InvalidProgramException("Erreur de l'ouverture de la session");

        }

        public TwainCore(int sourceIndex) : this()
        {
            try
            {
                if (!GetDataSources()[sourceIndex].IsOpen)
                {
                    _dataSources[sourceIndex].Open();

                    _dataSources[sourceIndex].DS.Capabilities.ICapXferMech.SetValue(XferMech.Native);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Problem occured,cannot communicate with scan device.");
            }
        }

        #endregion Constructor

        #region Event Handlers
        private void DataTransferred()
        {
            _twainSession.DataTransferred += async (s, e) =>
            {
                if (e.NativeData != IntPtr.Zero)
                {
                    Console.WriteLine("SUCCESS! Got twain data");

                    System.Windows.Media.Imaging.BitmapSource img = null;

                    switch (e.TransferType)
                    {
                        case XferMech.Native:
                            using (var stream = e.GetNativeImageStream())
                            {
                                if (stream != null)
                                {
                                    img = stream.ConvertToWpfBitmap(720, 0);
                                    await ReadAndUploadImage(img);

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
                }
                else
                {
                    Console.WriteLine("BUMMER! No twain data ");
                }
            };
        }

        private void _twainSession_TransferReady(object sender, TransferReadyEventArgs e)
        {
        }

        private void _twainSession_StateChanged(object sender, EventArgs e)
        {
            State = _twainSession.State;
            StateChanged?.Invoke(this, new StateChangedArgs() { NewState = State });
        }

        private void _twainSession_SourceDisabled(object sender, EventArgs e)
        {
            isComplete = true;
            SourceDisabled?.Invoke(this , null);
        }

        #endregion Event Handlers

        #region Public Methods

        public Task<string> ScanDocumentAsync(IntPtr Handle)
        {
            var tcs = new TaskCompletionSource<string>();

            var fileResult = string.Empty;

            EventHandler<DataTransferredEventArgs> eventHandler = null;

            eventHandler = (sender, e) =>
            {
                //Avoid memory leaks
                _twainSession.DataTransferred -= eventHandler;

                tcs.TrySetResult(displayfile);
            };

            _twainSession.DataTransferred += eventHandler;

            _twainSession.TransferError += (sender, e) =>
            {
                tcs.TrySetException(new Exception("Error occured during scan"));
            };

            if (_twainSession.State == 4)
                _twainSession.CurrentSource.Enable(SourceEnableMode.ShowUI, false, Handle);

            return tcs.Task;
        }

        public async Task ReadAndUploadImage(BitmapSource bitmapSource)
        {
            var imageBytes = ImageUploader.ConvertImageFromBitMapToBytes(bitmapSource);
            var imageName = $"scanned_image_{UploadImgCount++}.png";

            var response = await ImageUploader.UploadImage(imageBytes, imageName, folderName);

            if (response.IsSuccessStatusCode)
            {
                // await mainWindowViewModel._connection.SendAsync("ImageUploaded", $"{folderName}/{imageName}");
                Console.WriteLine("Image uploaded successfully.");
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }
        }

        #endregion Public Methods
        public class StateChangedArgs : EventArgs
        {
            public int NewState { get; set; }
        }
    }
}