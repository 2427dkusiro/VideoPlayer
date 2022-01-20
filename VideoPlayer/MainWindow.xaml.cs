using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var presentationSource = PresentationSource.FromVisual(this);
            Matrix matrix = presentationSource.CompositionTarget.TransformFromDevice;
            var dpiX = (int)Math.Round(96 * (1 / matrix.M11));
            var dpiY = (int)Math.Round(96 * (1 / matrix.M22));

            var viewModel = new MainWindowViewModel(new MainWindowViewModel.MainWindowViewModelContext()
            {
                DpiX = dpiX,
                DpiY = dpiY,
            });
            DataContext = viewModel;

            await viewModel.DebugMain();
            MessageBox.Show("再生終了");
        }
    }
}
