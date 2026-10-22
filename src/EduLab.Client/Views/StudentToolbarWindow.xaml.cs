using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using EduLab.Client.Services;
using Microsoft.Win32;

namespace EduLab.Client.Views
{
    public partial class StudentToolbarWindow : Window
    {
        private string _studentName;
        private string _className;

        public StudentToolbarWindow(string studentName, string className)
        {
            InitializeComponent();
            _studentName = studentName;
            _className = className;

            UpdateStudentInfo(studentName, className);

            Loaded += (s, e) => PositionAtTopCenter();
            MouseDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };

            // Hỗ trợ kéo thả file trực tiếp vào thanh toolbar để nộp bài
            AllowDrop = true;
            Drop += StudentToolbarWindow_Drop;

            ClientNetworkManager.Instance.OnFileReceived += (name, path) =>
            {
                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text = $"Nhận đề bài: {name}";
                    MessageBox.Show($"Thầy cô vừa gửi đề bài: {name}\nFile đã lưu tại: Desktop\\BaiTap_Tu_GiaoVien", "Nhận tài liệu mới", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            };
        }

        public void UpdateStudentInfo(string studentName, string className)
        {
            _studentName = string.IsNullOrWhiteSpace(studentName) ? "Chưa đăng ký" : studentName;
            _className = string.IsNullOrWhiteSpace(className) ? "---" : className;
            TxtStudentBadge.Text = $"{_studentName} ({_className})";
        }

        private void PositionAtTopCenter()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            Left = (screenWidth - Width) / 2;
            Top = 15;
        }

        private async void StudentToolbarWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    await SubmitSingleFileAsync(files[0]);
                }
            }
        }

        private async void BtnSubmitFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Chọn bài tập cần nộp cho giáo viên",
                Filter = "Tất cả các tệp (*.*)|*.*|Tệp văn bản (*.docx;*.doc;*.pdf)|*.docx;*.doc;*.pdf|Mã nguồn (*.py;*.cpp;*.pas;*.cs)|*.py;*.cpp;*.pas;*.cs"
            };

            if (dlg.ShowDialog() == true)
            {
                await SubmitSingleFileAsync(dlg.FileName);
            }
        }

        private async System.Threading.Tasks.Task SubmitSingleFileAsync(string filePath)
        {
            if (!File.Exists(filePath)) return;

            string fileName = Path.GetFileName(filePath);
            TxtStatus.Text = $"Đang nộp: {fileName}...";
            BtnSubmitFile.IsEnabled = false;

            try
            {
                await ClientNetworkManager.Instance.SubmitFileAsync(filePath, _studentName, "", _className);
                TxtStatus.Text = $"Đã nộp thành công: {fileName}";
                MessageBox.Show($"Đã nộp bài {fileName} thành công về máy giáo viên!", "Nộp bài thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Lỗi khi nộp bài!";
                MessageBox.Show($"Không thể nộp bài: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnSubmitFile.IsEnabled = true;
            }
        }
    }
}
