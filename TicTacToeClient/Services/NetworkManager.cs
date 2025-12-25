using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToeClient.Services
{
    /// <summary>
    /// Handles all network communication with the server
    /// </summary>
    public class NetworkManager
    {
        private TcpClient? client;
        private StreamWriter? writer;
        private StreamReader? reader;
        private bool isRunning;

        public event Action<string>? MessageReceived;
        public event Action<string>? ErrorOccurred;
        public event Action? ConnectionLost;

        public bool IsConnected => client?.Connected ?? false;

        /// <summary>
        /// Connects to the game server
        /// </summary>
        public async Task<bool> ConnectAsync(string serverIP, int port)
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(serverIP, port);

                NetworkStream stream = client.GetStream();
                writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                reader = new StreamReader(stream, Encoding.UTF8);

                isRunning = true;
                _ = Task.Run(ListenToServerAsync);

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Connection failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Continuously listens for messages from the server
        /// </summary>
        private async Task ListenToServerAsync()
        {
            try
            {
                while (isRunning && client?.Connected == true)
                {
                    string? message = await reader!.ReadLineAsync();
                    if (message == null) break;

                    MessageReceived?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                if (isRunning)
                {
                    ErrorOccurred?.Invoke($"Connection lost: {ex.Message}");
                }
            }
            finally
            {
                ConnectionLost?.Invoke();
            }
        }

        /// <summary>
        /// Sends a restart request to the server
        /// </summary>
        public async Task<bool> SendRestartAsync()
        {
            try
            {
                if (writer != null && IsConnected)
                {
                    await writer.WriteLineAsync("RESTART");
                    await writer.FlushAsync();
                    return true;
                }
                else
                {
                    ErrorOccurred?.Invoke("Not connected to server");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to send restart: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends player name to the server
        /// </summary>
        public async Task<bool> SendNameAsync(string playerName)
        {
            try
            {
                if (writer != null && IsConnected)
                {
                    await writer.WriteLineAsync($"NAME:{playerName}");
                    await writer.FlushAsync();
                    return true;
                }
                else
                {
                    ErrorOccurred?.Invoke("Not connected to server");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to send name: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends a chat message to the server
        /// </summary>
        public async Task<bool> SendChatAsync(string messageId, string message)
        {
            try
            {
                if (writer != null && IsConnected)
                {
                    await writer.WriteLineAsync($"CHAT:{messageId}:{message}");
                    await writer.FlushAsync();
                    return true;
                }
                else
                {
                    ErrorOccurred?.Invoke("Not connected to server");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to send chat: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends a video call request to the opponent
        /// </summary>
        public async Task<bool> SendCallRequestAsync()
        {
            try
            {
                if (writer != null && IsConnected)
                {
                    await writer.WriteLineAsync("CALL_REQUEST");
                    await writer.FlushAsync();
                    return true;
                }
                else
                {
                    ErrorOccurred?.Invoke("Not connected to server");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to send call request: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Accepts an incoming video call
        /// </summary>
        public async Task<bool> SendCallAcceptAsync()
        {
            try
            {
                if (writer != null && IsConnected)
                {
                    await writer.WriteLineAsync("CALL_ACCEPT");
                    await writer.FlushAsync();
                    return true;
                }
                else
                {
                    ErrorOccurred?.Invoke("Not connected to server");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to send call accept: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends video connection info (port) to server
        /// </summary>
        public async Task<bool> SendCallInfoAsync(int port)
        {
            try
            {
                if (writer != null && IsConnected)
                {
                    await writer.WriteLineAsync($"CALL_INFO:{port}");
                    await writer.FlushAsync();
                    return true;
                }
                else
                {
                    ErrorOccurred?.Invoke("Not connected to server");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to send call info: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Rejects an incoming video call
        /// </summary>
        public async Task<bool> SendCallRejectAsync()
        {
            try
            {
                if (writer != null && IsConnected)
                {
                    await writer.WriteLineAsync("CALL_REJECT");
                    await writer.FlushAsync();
                    return true;
                }
                else
                {
                    ErrorOccurred?.Invoke("Not connected to server");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to send call reject: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ends the current video call
        /// </summary>
        public async Task<bool> SendCallEndAsync()
        {
            try
            {
                if (writer != null && IsConnected)
                {
                    await writer.WriteLineAsync("CALL_END");
                    await writer.FlushAsync();
                    return true;
                }
                else
                {
                    ErrorOccurred?.Invoke("Not connected to server");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to send call end: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends a move to the server
        /// </summary>
        public async Task<bool> SendMoveAsync(int cellIndex)
        {
            System.Diagnostics.Debug.WriteLine($"[NETWORK] SendMoveAsync called with cellIndex: {cellIndex}");
            System.Diagnostics.Debug.WriteLine($"[NETWORK] Writer is null: {writer == null}");
            System.Diagnostics.Debug.WriteLine($"[NETWORK] IsConnected: {IsConnected}");
            
            try
            {
                if (writer != null && IsConnected)
                {
                    string message = $"MOVE:{cellIndex}";
                    System.Diagnostics.Debug.WriteLine($"[NETWORK] Sending message: {message}");
                    
                    await writer.WriteLineAsync(message);
                    await writer.FlushAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"[NETWORK] Message sent successfully");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[NETWORK] Cannot send - writer is null or not connected");
                    ErrorOccurred?.Invoke("Not connected to server");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NETWORK] Exception in SendMoveAsync: {ex.Message}");
                ErrorOccurred?.Invoke($"Failed to send move: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disconnects from the server
        /// </summary>
        public void Disconnect()
        {
            isRunning = false;

            try
            {
                writer?.Close();
                reader?.Close();
                client?.Close();
            }
            catch { }

            client = null;
            writer = null;
            reader = null;
        }
    }
}
