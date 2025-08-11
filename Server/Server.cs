using System.Net;
using System.Net.Sockets;

namespace Server
{
    public class Server
    {
        private readonly TcpListener _listener;
        private volatile bool _isRunning;

        private TcpClient _client;
        private StreamReader _reader;
        private StreamWriter _writer;

        private readonly Queue<string> _incomingMessages = new Queue<string>();
        private readonly object _incomingLock = new object();

        private readonly Queue<string> _outgoingMessages = new Queue<string>();
        private readonly object _outgoingLock = new object();

        public Server(string ipAddress, int port)
        {
            _listener = new TcpListener(IPAddress.Parse(ipAddress), port);
        }

        public void Start()
        {
            _listener.Start();
            _isRunning = true;

            var acceptThread = new Thread(AcceptClient);
            acceptThread.Start();

            Console.WriteLine("Сервер запущен.");
        }

        private void AcceptClient()
        {
            try
            {
                _client = _listener.AcceptTcpClient();
                _reader = new StreamReader(_client.GetStream());
                _writer = new StreamWriter(_client.GetStream()) { AutoFlush = true };
                Console.WriteLine($"Клиент подключен: {_client.Client.RemoteEndPoint}");

                _listener.Stop();

                var receiveThread = new Thread(ReceiveMessages);
                receiveThread.Start();

                var processThread = new Thread(ProcessMessages);
                processThread.Start();

                var sendThread = new Thread(SendMessages);
                sendThread.Start();
            }
            catch (SocketException)
            {
                Console.WriteLine("Ошибка при подключении клиента.");
            }
        }

        private void WaitForNewClient()
        {
            _reader?.Dispose();
            _writer?.Dispose();
            _client?.Close();

            Console.WriteLine("Ожидание нового клиента...");
            try
            {
                _listener.Start();
                _client = _listener.AcceptTcpClient();
                _reader = new StreamReader(_client.GetStream());
                _writer = new StreamWriter(_client.GetStream()) { AutoFlush = true };
                Console.WriteLine($"Клиент подключен: {_client.Client.RemoteEndPoint}");

                var receiveThread = new Thread(ReceiveMessages);
                receiveThread.Start();

                var processThread = new Thread(ProcessMessages);
                processThread.Start();

                var sendThread = new Thread(SendMessages);
                sendThread.Start();
            }
            catch (SocketException)
            {
                Console.WriteLine("Ошибка при подключении клиента.");
            }
        }

        private void ReceiveMessages()
        {
            while (_isRunning && _client?.Connected == true)
            {
                try
                {
                    var text = _reader.ReadLine();
                    if (text == null)
                    {
                        Console.WriteLine("Клиент отключился.");
                        break;
                    }
                    lock (_incomingLock)
                    {
                        _incomingMessages.Enqueue(text);
                    }
                    Console.WriteLine($"[Получено] {text}");
                }
                catch (IOException)
                {
                    Console.WriteLine("Ошибка чтения от клиента.");
                    break;
                }
            }
            WaitForNewClient();
        }

        private void ProcessMessages()
        {
            while (_isRunning)
            {
                string messageToProcess = null;
                lock (_incomingLock)
                {
                    if (_incomingMessages.Count > 0)
                    {
                        messageToProcess = _incomingMessages.Dequeue();
                    }
                }

                if (messageToProcess != null)
                {
                    Console.WriteLine($"[Обработка] Взято сообщение '{messageToProcess}'.");
                    var processedText = $"echo- {messageToProcess}";
                    lock (_outgoingLock)
                    {
                        _outgoingMessages.Enqueue(processedText);
                    }
                }
                else
                {
                    Thread.Sleep(50);
                }
            }
        }

        private void SendMessages()
        {
            while (_isRunning && _client?.Connected == true)
            {
                string messageToSend = null;
                lock (_outgoingLock)
                {
                    if (_outgoingMessages.Count > 0)
                    {
                        messageToSend = _outgoingMessages.Dequeue();
                    }
                }

                if (messageToSend != null)
                {
                    try
                    {
                        _writer.WriteLine(messageToSend);
                    }
                    catch (IOException)
                    {
                        Console.WriteLine("Ошибка отправки клиенту.");
                        Stop();
                        break;
                    }
                }
                else
                {
                    Thread.Sleep(50);
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            _reader?.Close();
            _writer?.Close();
            _client?.Close();
            Console.WriteLine("Сервер остановлен.");
        }
    }
}