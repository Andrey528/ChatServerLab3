using System.Net.Sockets;
using System.Net;
using ChatServerLab3;
using System.Text;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

class Program
{
    static TcpListener listener = new TcpListener(IPAddress.Any, 5050);
    static List<ConnectedClient> clients = new List<ConnectedClient>();
    static Regex regex = new Regex(@"FROM: (.*?), TO: (.*?), BODY: (.*)");

    static void Main(string[] args)
    {
        listener.Start();

        while (true)
        {
            var client = listener.AcceptTcpClient();

            Task.Factory.StartNew(async () =>
            {
                var sr = new StreamReader(client.GetStream());

                while (client.Connected) // Если клиент подключен
                {
                    var line = sr.ReadLine(); // Читаем отправленную им строку

                    if (line.Contains("Login: ") && !string.IsNullOrWhiteSpace(line.Replace("Login: ", ""))) // Есть ли логин клиента?
                    {
                        var nick = line.Replace("Login: ", ""); // Берем ник клиента

                        if (clients.FirstOrDefault(s => s.Name == nick) == null) // Есть ли такой ник среди клиентов?
                        {
                            clients.Add(new ConnectedClient(client, nick)); // Если такого клиента нет, то мы его добавляем
                            
                            Console.WriteLine($"New connection: {nick}"); // Лог сервера - говорим, что клиент подключен
                            ShowConnectedClients();

                            await SendConnectedClients();

                            break;
                        }
                        else
                        {
                            var sw = new StreamWriter(client.GetStream());
                            sw.AutoFlush = true;

                            sw.WriteLine("Пользователь с таким ником уже есть в чате");
                            client.Client.Disconnect(false); // Если клиент с таким ником уже есть, то мы его отключаем
                        }
                    }


                }

                while (client.Connected)
                {
                    try
                    {
                        sr = new StreamReader(client.GetStream());
                        var line = sr.ReadLine();

                        if (line == "Disconnect") // Проверяем, если клиент отключился
                        {
                            var disconnectedClient = clients.FirstOrDefault(c => c.Client == client);
                            clients.Remove(disconnectedClient); // Удаляем отключенного клиента из списка

                            ShowConnectedClients();

                            await SendConnectedClients(); // Отправляем обновленный список клиентов всем клиентам

                            // Закрыть соединение и освободить ресурсы
                            client.GetStream().Close();
                            client.Close();
                            client.Dispose();
                            break;
                        }

                        Match match = regex.Match(line);

                        if (match.Success) // Регулярное выражение проверяет на шаблон персонального сообщения
                        {
                            string sender = match.Groups[1].Value.Trim();
                            string recipient = match.Groups[2].Value.Trim();
                            string message = match.Groups[3].Value.Trim();

                            await SendPersonalMessage(sender, recipient, message);
                        }
                        else
                        {
                            await SendToAllClients(line); // Если клиент подключен и когда прийдет строчка сообщения на сервер, то мы отправим сообщение пользователям
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex.Message);
                    }
                }
            });
        }

        static async Task SendPersonalMessage(string sender, string recipient, string message)
        {
            var recipientClient = clients.FirstOrDefault(c => c.Name == recipient); // Находим клиента по нику получателя


            if (recipientClient != null && recipientClient.Client.Connected) // Если клиент существует и подключен
            {
                var sw = new StreamWriter(recipientClient.Client.GetStream());
                sw.AutoFlush = true;
                await sw.WriteLineAsync($"Personal message from {sender}: {message}"); // Отправляем сообщение клиенту
            }
        }

        static async Task SendToAllClients(string message)
        {
            Console.WriteLine(message);

            foreach (var client in clients)
            {
                Console.WriteLine(message);

                var sw = new StreamWriter(client.Client.GetStream());
                await sw.WriteLineAsync(message); //передача данных
                await sw.FlushAsync();
            }
        }

        static async Task SendConnectedClients()
        {
            var connectedClients = string.Join(",", clients.Select(c => c.Name)); // Соединяем имена клиентов через запятую
            var message = $"ConnectedClients: {connectedClients}";

            foreach (var client in clients)
            {
                var sw = new StreamWriter(client.Client.GetStream());
                await sw.WriteLineAsync(message);
                await sw.FlushAsync(); // Очищаем буфер
            }
        }

        static void ShowConnectedClients()
        {
            Console.WriteLine("Remaining connected clients: "); // Вывод в консоль на сервер подключенных клиентов
            foreach (var connectedClient in clients)
            {
                Console.WriteLine("- " + connectedClient.Name);
            }
        }
    }
}