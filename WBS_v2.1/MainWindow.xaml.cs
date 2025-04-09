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
using WebSocketSharp;
using System.IO;
using System.Timers;
using Microsoft.Win32;
using System.Globalization;
using OfficeOpenXml;
using System.ComponentModel;

// Установка контекста лицензии

namespace WBS_v2._1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WebSocket _webSocket;
        private List<double> _dataBuffer = new List<double>();
        private string _excelFilePath;
        private bool _isConnected = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string url = txtWebSocketUrl.Text;
                _excelFilePath = txtExcelFilePath.Text;
                _webSocket = new WebSocket(url);
                _webSocket.OnMessage += OnMessageReceived;
                _webSocket.Connect();
                _isConnected = true;
                btnSaveData.IsEnabled = true;
                MessageBox.Show("Успешное подключение.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            Console.WriteLine($"Полученные данные: {e.Data}");

            // Пробуем преобразовать данные с учетом культурной информации
            if (double.TryParse(e.Data, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
            {
                _dataBuffer.Add(value);

                // Вычисляем медиану
                double median = CalculateMedian(_dataBuffer);

                // Добавляем данные в ListBox
                Dispatcher.Invoke(() =>
                {
                    lstData.Items.Add($"Значение: {value}, Медиана: {median}");
                });
            }
            else
            {
                Console.WriteLine($"Не удалось преобразовать данные: {e.Data}");
            }
        }

        private void BtnSaveData_Click(object sender, RoutedEventArgs e)
        {
            if (_dataBuffer.Count > 0)
            {
                // Проверка, указан ли путь к файлу
                if (string.IsNullOrWhiteSpace(txtExcelFilePath.Text))
                {
                    MessageBox.Show("Пожалуйста, укажите путь к файлу Excel.");
                    return;
                }

                double median = CalculateMedian(_dataBuffer);
                TimeSpan duration = TimeSpan.FromMilliseconds(_dataBuffer.Count * 300); // Примерная продолжительность

                try
                {
                    SaveToExcel(txtExcelFilePath.Text, DateTime.Now, median, duration.TotalMilliseconds);
                    MessageBox.Show("Данные успешно сохранены в Excel.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении данных в Excel: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Нет данных для сохранения.");
            }
        }

        private double CalculateMedian(List<double> data)
        {
            if (data == null || data.Count == 0)
                throw new InvalidOperationException("Нет данных для вычисления медианы.");

            var sortedData = data.OrderBy(n => n).ToList();
            int count = sortedData.Count;
            double median;

            if (count % 2 == 0) // Если четное количество элементов
            {
                median = (sortedData[count / 2 - 1] + sortedData[count / 2]) / 2.0;
            }
            else // Если нечетное количество элементов
            {
                median = sortedData[count / 2];
            }

            return median;
        }

        private void SaveToExcel(string filePath, DateTime timestamp, double median, double duration)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Data");

                worksheet.Cells[1, 1].Value = "Timestamp";
                worksheet.Cells[1, 2].Value = "Median";
                worksheet.Cells[1, 3].Value = "Duration (ms)";

                worksheet.Cells[2, 1].Value = timestamp;
                worksheet.Cells[2, 2].Value = median;
                worksheet.Cells[2, 3].Value = duration;

                package.SaveAs(new FileInfo(filePath));
            }
        }

        private void BtnSelectExcelFile_Click(object sender, RoutedEventArgs e)
        {
            // Создаем экземпляр OpenFileDialog
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xls;*.xlsx)|*.xls;*.xlsx|All Files (*.*)|*.*", // Фильтр для файлов
                Title = "Выберите Excel файл"
            };

            // Отображаем диалог и проверяем результат
            if (openFileDialog.ShowDialog() == true)
            {
                // Записываем путь к выбранному файлу в TextBox
                txtExcelFilePath.Text = openFileDialog.FileName;
            }
        }
    }
}