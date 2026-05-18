using System.Net;
using System.Net.Sockets;

namespace EchoServer
{
    /// <summary>
    /// This program was designed for test purposes only
    /// Not for a review
    /// </summary>
    public class Program
    {
        private TcpListener? _listener;

        private readonly int _port;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public Program(int port)
        {
            _port = port;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public static async Task Main(string[] args)
        {
            Program server = new Program(5000);

            // Start the server in a separate task
            await Task.Run(server.StartAsync);

            string host = "127.0.0.1"; // Target IP
            int port = 60000;          // Target Port
            int intervalMilliseconds = 5000; // Send every 3 seconds

            using var sender = new UdpTimedSender(host, port);

            Console.WriteLine("Press any key to stop sending...");
            sender.StartSending(intervalMilliseconds);

            Console.WriteLine("Press 'q' to quit...");

            while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q)
            {
                // Just wait until 'q' is pressed
            }

            sender.StopSending();
            server.Stop();
            Console.WriteLine("Sender stopped.");

        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Console.WriteLine($"Server started on port {_port}.");

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("Client connected.");

                    _ = Task.Run(() => HandleClientAsync(client, _cancellationTokenSource.Token));
                }
                catch (ObjectDisposedException)
                {
                    // Listener has been closed
                    break;
                }
            }

            Console.WriteLine("Server shutdown.");
        }

        private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using NetworkStream stream = client.GetStream();

            try
            {
                const int bufferSize = 8192;
                byte[] buffer = new byte[bufferSize];
                int bytesRead;

                while (!token.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    // Echo back the received message
                    await stream.WriteAsync(buffer, 0, bytesRead, token);
                    Console.WriteLine($"Echoed {bytesRead} bytes to the client.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener?.Stop();
            _cancellationTokenSource.Dispose();
            Console.WriteLine("Server stopped.");
        }
    }
}