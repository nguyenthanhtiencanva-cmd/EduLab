using System.Windows;
using System.Windows.Input;
using EduLab.Server.ViewModels;

namespace EduLab.Server.Views
{
    public partial class ScreenMatrixWindow : Window
    {
        public ScreenMatrixWindow(MainViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) Close();
            };
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ScreenItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is FrameworkElement el && el.DataContext is ClientItemViewModel client)
            {
                // Mở cửa sổ xem chi tiết riêng màn hình này
                var zoomWin = new Window
                {
                    Title = $"Xem màn hình chi tiết - {client.DisplayName} ({client.StudentName})",
                    Width = 1024,
                    Height = 650,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = System.Windows.Media.Brushes.Black,
                    Content = new System.Windows.Controls.Image
                    {
                        Source = client.ThumbnailSource,
                        Stretch = System.Windows.Media.Stretch.Uniform
                    }
                };
                zoomWin.ShowDialog();
            }
        }
    }
}
