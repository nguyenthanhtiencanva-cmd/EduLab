using System.Windows;

namespace EduLab.Server.Views
{
    public partial class BroadcastDialog : Window
    {
        public string? BroadcastText { get; private set; }

        public BroadcastDialog()
        {
            InitializeComponent();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            BroadcastText = TxtMessage.Text.Trim();
            DialogResult = true;
            Close();
        }
    }
}
