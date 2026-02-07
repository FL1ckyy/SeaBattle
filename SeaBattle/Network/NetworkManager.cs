using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SeaBattle.Enums;

namespace SeaBattle.Network
{
    public class NetworkManager
    {
        private TcpListener server;
        private TcpClient client;
        private NetworkStream stream;
        private GameManager gameManager;

        public bool IsConnected { get; private set; }
        public bool IsHost { get; private set; }

        public event Action<string> MessageReceived;
        public event Action<string> StatusChanged;

        public NetworkManager(GameManager manager)
        {
            gameManager = manager;
            IsConnected = false;
        }

        public async Task StartServer(int port)
        {
            try
            {
                server = new TcpListener(IPAddress.Any, port);
                server.Start();
                IsHost = true;

                StatusChanged?.Invoke($"Сервер запущен на порту {port}. Ожидание подключения...");

                client = await server.AcceptTcpClientAsync();
                stream = client.GetStream();
                IsConnected = true;

                StatusChanged?.Invoke("Игрок подключен!");
                MessageReceived?.Invoke("Ожидайте начала игры...");

                // Начинаем прослушивание сообщений
                _ = Task.Run(ReceiveMessages);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Ошибка сервера: {ex.Message}");
            }
        }

        public async Task ConnectToServer(string ip, int port)
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(ip, port);
                stream = client.GetStream();
                IsConnected = true;
                IsHost = false;

                StatusChanged?.Invoke($"Подключено к {ip}:{port}");
                MessageReceived?.Invoke("Ожидайте начала игры...");

                // Начинаем прослушивание сообщений
                _ = Task.Run(ReceiveMessages);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Ошибка подключения: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            stream?.Close();
            client?.Close();
            server?.Stop();
            IsConnected = false;
        }

        public void SendMessage(MessageType type, object data)
        {
            if (!IsConnected) return;

            try
            {
                var message = new GameMessage(type, data);
                string json = message.ToJson();
                byte[] buffer = Encoding.UTF8.GetBytes(json + "\n");
                stream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Ошибка отправки: {ex.Message}");
            }
        }

        public void SendShot(int x, int y)
        {
            SendMessage(MessageType.Shot, new ShotData { X = x, Y = y });
        }

        public void SendShotResult(int x, int y, CellState result)
        {
            SendMessage(MessageType.ShotResult, new ShotResultData
            {
                X = x,
                Y = y,
                Result = result
            });
        }

        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[1024];

            try
            {
                while (IsConnected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var messages = json.Split('\n');

                    foreach (var msg in messages)
                    {
                        if (!string.IsNullOrWhiteSpace(msg))
                        {
                            ProcessMessage(msg.Trim());
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Соединение разорвано
            }
            finally
            {
                Disconnect();
                StatusChanged?.Invoke("Соединение разорвано");
            }
        }

        private void ProcessMessage(string json)
        {
            try
            {
                var message = GameMessage.FromJson(json);

                switch (message.Type)
                {
                    case MessageType.Shot:
                        var shotData = JsonSerializer.Deserialize<ShotData>(message.Data.ToString());
                        gameManager.ProcessShot(shotData.X, shotData.Y, false);
                        break;

                    case MessageType.ShotResult:
                        var resultData = JsonSerializer.Deserialize<ShotResultData>(message.Data.ToString());
                        // Обновляем поле врага
                        gameManager.EnemyBoard.Cells[resultData.X, resultData.Y].State = resultData.Result;
                        break;

                    case MessageType.StartGame:
                        gameManager.StartGame();
                        break;

                    case MessageType.GameOver:
                        gameManager.ChangeState(GameState.GameOver);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageReceived?.Invoke($"Ошибка обработки сообщения: {ex.Message}");
            }
        }
    }
}