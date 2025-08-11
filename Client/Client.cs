using System.Net.Sockets;

namespace Client
{
    public class Client
    {
        private readonly string _serverIp;
        private readonly int _serverPort;
        private TcpClient _client;
        private NetworkStream _stream;
        private volatile bool _isConnected;

        public Client(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
        }

        public void Connect()
        {
            try
            {
                _client = new TcpClient(_serverIp, _serverPort);
                _stream = _client.GetStream();
                _isConnected = true;
                
                Console.WriteLine("Успешно подключен к серверу.");
                Console.WriteLine("Вводите сообщения для отправки. Для выхода введите 'exit'.");

                Thread sendThread = new Thread(SendMessages);
                sendThread.Start();

                Thread receiveThread = new Thread(ReceiveMessages);
                receiveThread.Start();

                sendThread.Join();
            }
            catch (SocketException)
            {
                Console.WriteLine("Не удалось подключиться к серверу. Убедитесь, что сервер запущен.");
            }
            finally
            {
                Disconnect();
            }
        }
        
        private void SendMessages()
        {
            using (var writer = new StreamWriter(_stream))
            {
                while (_isConnected)
                {
                    string message = Console.ReadLine();
                    if (string.Equals(message, "exit", StringComparison.OrdinalIgnoreCase))
                    {
                        _isConnected = false;
                        break;
                    }

                    try
                    {
                        writer.WriteLine(message);
                        writer.Flush();
                    }
                    catch (IOException)
                    {
                        Console.WriteLine("Сервер разорвал соединение.");
                        _isConnected = false;
                    }
                }
            }
            _isConnected = false;
        }
        
        private void ReceiveMessages()
        {
            using (var reader = new StreamReader(_stream))
            {
                while (_isConnected)
                {
                    try
                    {
                        if (_client.GetStream().DataAvailable)
                        {
                             var response = reader.ReadLine();
                             if (response == null)
                             {
                                 _isConnected = false;
                                 Console.WriteLine("Соединение с сервером потеряно.");
                                 break;
                             }
                             Console.WriteLine($"Ответ сервера: {response}");
                        } else {
                            Thread.Sleep(100);
                        }
                    }
                    catch (IOException)
                    {
                        break;
                    }
                }
            }
        }
        
        private void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
        }
    }
}