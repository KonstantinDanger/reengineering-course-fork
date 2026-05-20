using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace NetSdrClientApp.Networking
{
    public class UdpClientWrapper : IUdpClient
    {
        private readonly IPEndPoint _localEndPoint;
        private CancellationTokenSource? _cts;
        private UdpClient? _udpClient;

        public event EventHandler<byte[]>? MessageReceived;

        public UdpClientWrapper(int port) => _localEndPoint = new IPEndPoint(IPAddress.Any, port);

        public async Task StartListeningAsync()
        {
            _cts = new CancellationTokenSource();
            Console.WriteLine("Start listening for UDP messages...");

            try
            {
                _udpClient = new UdpClient(_localEndPoint);
                while (!_cts.Token.IsCancellationRequested)
                {
                    UdpReceiveResult result = await _udpClient.ReceiveAsync(_cts.Token);
                    MessageReceived?.Invoke(this, result.Buffer);

                    Console.WriteLine($"Received from {result.RemoteEndPoint}");
                }
            }
            catch (OperationCanceledException)
            {
                //empty
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
            }
        }

        public void StopListening()
        {
            try
            {
                _cts?.Cancel();
                _udpClient?.Close();
                Console.WriteLine("Stopped listening for UDP messages.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while stopping: {ex.Message}");
            }
        }

        public void Exit() => StopListening();

        public override int GetHashCode()
        {
            string payload = $"{nameof(UdpClientWrapper)}|{_localEndPoint.Address}|{_localEndPoint.Port}";
            byte[] inputBytes = Encoding.UTF8.GetBytes(payload);
            byte[] hash = SHA512.HashData(inputBytes);

            return BitConverter.ToInt32(hash, 0);
        }
    }
}