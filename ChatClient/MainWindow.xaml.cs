using DevExpress.Mvvm;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChatClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _IP;
        public string IP { get => _IP; set => _IP = value; }

        private int _Port;
        public int Port { get => _Port; set => _Port = value; }

        private string _Nick;
        public string Nick { get => _Nick; set => _Nick = value; }

        private string _Chat;
        public string Chat { get => _Chat; set => _Chat = value; }

        private string _Message;
        public string Message { get => _Message; set => _Message = value; }

        private CancellationTokenSource _cancellationTokenSource;

        private ObservableCollection<string> _ConnectedClients = new ObservableCollection<string>();
        public ObservableCollection<string> ConnectedClients { get => _ConnectedClients; set => _ConnectedClients = value; }

        private TcpClient client;
        private StreamWriter sw;
        private StreamReader sr;

        public MainWindow()
        {
            InitializeComponent();
            IP = "192.168.0.10";
            Port = 5050;
            Nick = "Jack";

            txtIp.Text = IP;
            txtPort.Text = Port.ToString();
            txtNick.Text = Nick;
        }

        public void ConnectCommand(object sender, RoutedEventArgs e)
        {
            IP = txtIp.Text;
            Port = Convert.ToInt16(txtPort.Text);
            Nick = txtNick.Text;

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                client = new TcpClient();
                client.Connect(IP, Port);
                sr = new StreamReader(client.GetStream()); // Буффер для чтения сообщений
                sw = new StreamWriter(client.GetStream()); // Буффер для написания сообщений

                sw.AutoFlush = true; // Окончание работы с буфферами

                sw.WriteLine($"Login: {Nick}"); // Сразу как подключился, представляемся серверу

                Thread listenThread = new Thread(() => ReadMessages(_cancellationTokenSource.Token));
                listenThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void ReadMessages(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = sr.ReadLine();
                    // если пустой ответ, ничего не выводим на консоль
                    if (string.IsNullOrEmpty(line)) continue;

                    if (line.StartsWith("ConnectedClients: "))
                    {
                        string[] connectedClients = line.Replace("ConnectedClients: ", "").Split(',');
                        ObservableCollection<string> updatedConnectedClients = new ObservableCollection<string>();

                        foreach (var client in connectedClients)
                        {
                            updatedConnectedClients.Add(client);
                        }

                        Dispatcher.Invoke(() =>
                        {
                            ConnectedClients.Clear(); // Очищаем текущий список подключенных клиентов

                            foreach (var client in updatedConnectedClients)
                            {
                                ConnectedClients.Add(client);
                            }

                            clientsDataGrid.ItemsSource = ConnectedClients;
                        });
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            txtChat.Text += line + "\n";
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message);
                });
            }
            finally
            {
                try
                {
                    if (client != null && client.Connected)
                    {
                        client.GetStream().Close();
                        client.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public void SendMessage(object sender, RoutedEventArgs e) // Метод асинхронной отправки сообщения
        {
            Message = txtMessage.Text;

            if (client?.Connected == true && !string.IsNullOrWhiteSpace(Message))
            {
                var recipient = clientsDataGrid.SelectedItem;

                if (recipient != null)
                {
                    try
                    {
                        string personalMessage = $"FROM: {Nick}, TO: {recipient}, BODY: {Message}";
                        sw.WriteLine(personalMessage);
                        txtMessage.Text = ""; // Очищаем поле ввода сообщения
                        clientsDataGrid.SelectedItem = null; // Очищаем выбор пользователя из списка подключенных пользователей
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                } 
                else
                {
                    try
                    {
                        string message = $"{Nick}: {Message}";
                        sw.WriteLine(message);
                        txtMessage.Text = ""; // Очищаем поле ввода сообщения
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            try
            {
                _cancellationTokenSource?.Cancel();

                if (client?.Connected == true) // Проверяем, что клиент подключен
                {
                    sw.WriteLine("Disconnect"); // Отправляем серверу сообщение, что клиент отключается
                    client.GetStream().Close(); // Закрываем поток данных клиента
                    client.Close(); // Закрываем соединение
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}