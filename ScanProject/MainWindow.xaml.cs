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
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
