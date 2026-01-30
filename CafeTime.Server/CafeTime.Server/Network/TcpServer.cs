// Network/TcpServer.cs
using CafeTime.Server.Common.DOTs;  // Add this line
using CafeTime.Server.Network;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace CafeTime.Server.Network
{
    public class TcpServer
    {
        private TcpListener listener;
        private bool isRunning;

        public void Start(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            isRunning = true;

            Console.WriteLine($"Server started on port {port}");

            while (isRunning)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[4096];

                while (client.Connected)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string requestJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received: {requestJson}");

                    var request = JsonSerializer.Deserialize<Request>(requestJson);
                    var response = RequestHandler.HandleRequest(request);

                    string responseJson = JsonSerializer.Serialize(response);
                    byte[] responseData = Encoding.UTF8.GetBytes(responseJson);
                    stream.Write(responseData, 0, responseData.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client error: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        public void Stop()
        {
            isRunning = false;
            listener?.Stop();
        }
    }
}