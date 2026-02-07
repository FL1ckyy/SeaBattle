using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
        public event Action Connected;

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

                StatusChanged?.Invoke($"Сервер запущен на порту {port}...");

                client = await server.AcceptTcpClientAsync();
                stream = client.GetStream();
                IsConnected = true;

                StatusChanged?.Invoke("Игрок подключен!");
                MessageReceived?.Invoke("Соперник подключился!");
                Connected?.Invoke();

                _ = Task.Run(ReceiveMessages);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Ошибка: {ex.Message}");
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
                MessageReceived?.Invoke("Подключение успешно!");
                Connected?.Invoke();

                _ = Task.Run(ReceiveMessages);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Ошибка подключения: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            try
            {
                stream?.Close();
                client?.Close();
                server?.Stop();
                IsConnected = false;
            }
            catch { }
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

        public void SendShotResult(int x, int y, string result)
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
                while (IsConnected && client?.Connected == true)
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
            catch (IOException)
            {
                StatusChanged?.Invoke("Соединение разорвано");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Ошибка: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        private void ProcessMessage(string json)
        {
            try
            {
                var message = GameMessage.FromJson(json);

                switch (message.Type)
                {
                    case MessageType.StartGame:
                        MessageReceived?.Invoke("Игра началась!");
                        if (!IsHost)
                        {
                            gameManager.StartGameAsClient();
                        }
                        break;

                    case MessageType.Shot:
                        var shotData = JsonConvert.DeserializeObject<ShotData>(message.Data.ToString());
                        gameManager.ProcessIncomingShot(shotData.X, shotData.Y);
                        break;

                    case MessageType.ShotResult:
                        var resultData = JsonConvert.DeserializeObject<ShotResultData>(message.Data.ToString());
                        gameManager.ProcessShotResult(resultData.X, resultData.Y, resultData.Result);
                        break;

                    case MessageType.GameOver:
                        gameManager.ChangeState(GameState.GameOver);
                        MessageReceived?.Invoke("Вы победили!");
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageReceived?.Invoke($"Ошибка: {ex.Message}");
            }
        }
    }
}