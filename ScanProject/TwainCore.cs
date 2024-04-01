//using NTwain;
using NTwain;
using NTwain.Data;
using ScanProject.SignalR;
using ScanProject.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ScanProject
{
    public class AscTwainSession : TwainSession
    {
        public AscTwainSession(DataGroups supportedGroups) : base(supportedGroups)
        {
        }

        public AscTwainSession(TWIdentity appId) : base(appId)
        {
        }

        public void OnSourceDisabled()
        {
            base.OnSourceDisabled();
        }
    }
    public class TwainCore
    {
        #region Private Property

        private int _state;
        private readonly AscTwainSession _twainSession;
        private MessageClient messageClient;
        private ObservableCollection<DataSourceVM> _dataSources
            = new ObservableCollection<DataSourceVM>();

        private string _tempPath;
        private int count = 1;
        private string displayfile = "";
        public string folderName = "";
        #endregion Private Property
        IntPtr Handle;
        #region Public Property

        public event EventHandler<StateChangedArgs> StateChanged;

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
            // messageClient = new MessageClient();
            //messageClient.CreateConncetion();
            //Allow old Device DSM drives
            PlatformInfo.Current.PreferNewDSM = false;

            var appId = TWIdentity.CreateFromAssembly(DataGroups.Image, Assembly.GetExecutingAssembly());
            _twainSession = new AscTwainSession(appId);

            PlatformInfo.Current.PreferNewDSM = false;

            _twainSession.TransferReady += _twainSession_TransferReady;
            _twainSession.StateChanged += _twainSession_StateChanged;
            var tcs = new TaskCompletionSource<string>();/*
                                                          * 
            EventHandler<DataTransferredEventArgs> eventHandler = null;
            var fileResult = string.Empty;
            var fileName = $"doc{count}";
            var tcs = new TaskCompletionSource<string>();
            eventHandler = (sender, e) =>
            {
                //Avoid memory leaks
                //_twainSession.DataTransferred -= eventHandler;

                fileResult = TwainHelpers.ConvertImageFromBmpToJpg(_tempPath, @"E:\\", fileName);//Path.GetTempPath(), fileName);
                tcs.TrySetResult(fileResult);
            };

            _twainSession.DataTransferred += eventHandler;*/
            //

            _twainSession.DataTransferred += async (s, e) =>
             {
                 if (e.NativeData != IntPtr.Zero)
                 {
                     //Bitmap img = null;
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
                                     string name = SaveScannedImage(img);
                                     await ReadAndUploadImage(name);
                                     // tcs.TrySetResult(name);
                                 }
                             }

                             // tcs.TrySetResult(displayfile);
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

            /*   _twainSession.DataTransferred += (sender, e) =>
               {
                   //Avoid memory leaks

                   tcs.TrySetResult(displayfile);
               };*/

            //

            if (_twainSession.Open() != ReturnCode.Success)
                throw new InvalidProgramException("Erreur de l'ouverture de la session");

        }

        private void _twainSession_DataTransferred(object sender, DataTransferredEventArgs e)
        {
            MessageBox.Show(e.FileDataPath);
        }

        public TwainCore(int sourceIndex) : this()
        {
            try
            {
                if (!GetDataSources()[sourceIndex].IsOpen)
                {
                    _dataSources[sourceIndex].Open();
                    // _dataSources[sourceIndex].DS.Capabilities.ICapXferMech.SetValue(XferMech.File);
                    _dataSources[sourceIndex].DS.Capabilities.ICapXferMech.SetValue(XferMech.Native);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Problem occured,cannot communicate with scan device.");
            }

            // Handle = handle;

        }

        #endregion Constructor

        #region Event Handlers

        private void _twainSession_TransferReady(object sender, TransferReadyEventArgs e)
        {
            /*var mech = _twainSession.CurrentSource.Capabilities.ICapXferMech.GetCurrent();
            if (mech == XferMech.File)
            {
                var formats = _twainSession.CurrentSource.Capabilities.ICapImageFileFormat.GetValues();
                var wantFormat = formats.Contains(FileFormat.Tiff) ? FileFormat.Tiff : FileFormat.Bmp;

                var fileSetup = new TWSetupFileXfer
                {
                    Format = wantFormat,
                    FileName = Path.Combine(Path.GetTempPath(), $"tempDoc{++count}.{wantFormat}")
                };

                _tempPath = fileSetup.FileName;
                var rc = _twainSession.CurrentSource.DGControl.SetupFileXfer.Set(fileSetup);
            }*/
        }

        private void _twainSession_StateChanged(object sender, EventArgs e)
        {
            State = _twainSession.State;
            StateChanged?.Invoke(this, new StateChangedArgs() { NewState = State });
        }

        #endregion Event Handlers

        #region Public Methods

        public Task<string> ScanDocumentAsync(string directory, string fileName, IntPtr Handle)
        {
            var tcs = new TaskCompletionSource<string>();

            var fileResult = string.Empty;

            EventHandler<DataTransferredEventArgs> eventHandler = null;

            eventHandler = (sender, e) =>
            {
                //Avoid memory leaks
                _twainSession.DataTransferred -= eventHandler;

                //  fileResult = TwainHelpers.ConvertImageFromBmpToJpg(_tempPath, @"E:\\", fileName);//Path.GetTempPath(), fileName);
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

        public Task<string> ScanDocumentFromSignalrAsync(string directory, string fileName, IntPtr Handle)
        {
            var tcs = new TaskCompletionSource<string>();

            var fileResult = string.Empty;

            EventHandler<DataTransferredEventArgs> eventHandler = null;

            eventHandler = (sender, e) =>
            {
                //Avoid memory leaks
                //_twainSession.DataTransferred -= eventHandler;

                //  fileResult = TwainHelpers.ConvertImageFromBmpToJpg(_tempPath, @"E:\\", fileName);//Path.GetTempPath(), fileName);
                tcs.TrySetResult(displayfile);
            };

            _twainSession.DataTransferred += eventHandler;

            _twainSession.TransferError += (sender, e) =>
            {
                tcs.TrySetException(new Exception("Error occured during scan"));
            };

            if (_twainSession.State == 4)
                _twainSession.CurrentSource.Enable(SourceEnableMode.ShowUI, false, Handle);

            // _twainSession.OnSourceDisabled();

            return tcs.Task;
        }

        public void StartSource(IntPtr Handle)
        {
            if (_twainSession.State == 4)
                _twainSession.CurrentSource.Enable(SourceEnableMode.ShowUI, false, Handle);
        }

        string SaveScannedImage(BitmapSource img)
        {
            if (img == null)
            {
                Console.WriteLine("Image is null. Cannot save.");
                return " ";
            }

            // Create a file name for saving the image
            // string fileName = $"scanned_image_{DateTime.Now.ToString("yyyyMMddHHmmss")}.png";
            string fileName = $"scanned_image_{count++}.png";

            // Combine the save path with the file name
            string filePath = Path.Combine("E:\\", fileName);

            // Convert BitmapSource to Bitmap
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(img));

            // Save the image to the specified file path
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fileStream);
                Console.WriteLine("Image saved successfully: " + filePath);
            }

            displayfile = filePath;

            return filePath;
        }

        #endregion Public Methods
        private async Task ReadAndUploadImage(string imageUrl)
        {
            // Read the image file into a byte array
            byte[] imageBytes = File.ReadAllBytes(imageUrl);

            // Upload the image to the API
            await UploadImage(imageBytes);
        }

        private async Task UploadImage(byte[] imageBytes)
        {
            using (HttpClient client = new HttpClient())
            using (var formData = new MultipartFormDataContent())
            {
                // Set the API endpoint URL
                string apiUrl = "http://localhost:5029/api/SaveImage";

                // Add the image data to the form data
                formData.Add(new ByteArrayContent(imageBytes), "image", "image.jpg");
                //folderName
                formData.Add(new StringContent(folderName), "folderName");
                // Send the HTTP POST request to the API
                HttpResponseMessage response = await client.PostAsync(apiUrl, formData);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Image uploaded successfully.");
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                }
            }
        }

        public class StateChangedArgs : EventArgs
        {
            public int NewState { get; set; }
        }
    }
}