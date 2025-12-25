using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToeServer
{
    public class GameServer
    {
        private TcpListener? listener;
        private const int PORT = 5000;

        private char[] board = new char[9];
        private char currentPlayer = 'X';
        private bool gameActive = false;
        private object lockObj = new object();

        private Dictionary<char, TcpClient> playerMap = new Dictionary<char, TcpClient>();
        private Dictionary<char, StreamWriter> writerMap = new Dictionary<char, StreamWriter>();
        private Dictionary<char, string> playerNames = new Dictionary<char, string>(); // Player names

        public void Start()
        {
            // Get local LAN IP address
            string localIP = GetLocalIPAddress();
            
            listener = new TcpListener(IPAddress.Any, PORT);
            listener.Start();
            
            Console.WriteLine("===========================================");
            Console.WriteLine($"  SERVER STARTED ON PORT {PORT}");
            Console.WriteLine("===========================================");
            Console.WriteLine($"Local IP Address: {localIP}");
            Console.WriteLine($"Clients should connect to: {localIP}:{PORT}");
            Console.WriteLine("===========================================");
            Console.WriteLine("Waiting for players...\n");

            for (int i = 0; i < 9; i++) board[i] = ' ';

            AcceptClients();
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        private async void AcceptClients()
        {
            try
            {
                while (playerMap.Count < 2)
                {
                    TcpClient client = await listener!.AcceptTcpClientAsync();
                    NetworkStream stream = client.GetStream();
                    StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);

                    char playerSymbol;

                    Console.WriteLine($"[DEBUG] Before lock - playerMap count: {playerMap.Count}");
                    Console.WriteLine($"[DEBUG] playerMap contains X: {playerMap.ContainsKey('X')}, Y: {playerMap.ContainsKey('Y')}");

                    lock (lockObj)
                    {
                        playerSymbol = playerMap.ContainsKey('X') ? 'Y' : 'X';
                        
                        Console.WriteLine($"[DEBUG] Inside lock - Determined playerSymbol: {playerSymbol}");
                        
                        playerMap[playerSymbol] = client;
                        writerMap[playerSymbol] = writer;

                        Console.WriteLine($"[CONNECTION] Client connecting... Current player count: {playerMap.Count}");
                        Console.WriteLine($"[ASSIGNMENT] Assigning player symbol: {playerSymbol}");
                        Console.WriteLine($"[CONNECTED] Player {playerSymbol} connected successfully (Total players: {playerMap.Count}/2)");
                        
                        // Debug: Print all players in map
                        Console.WriteLine($"[DEBUG] Current players in map: {string.Join(", ", playerMap.Keys)}");
                    }

                    Console.WriteLine($"[DEBUG] After lock - playerMap count: {playerMap.Count}");

                    await writer.WriteLineAsync($"PLAYER:{playerSymbol}");

                    _ = Task.Run(() => HandleClient(client, reader, playerSymbol));

                    lock (lockObj)
                    {
                        if (playerMap.Count == 2 && !gameActive)
                        {
                            gameActive = true;
                            Console.WriteLine("[GAME START] Both players connected. Game starting!");
                            BroadcastMessage("START").Wait();
                            SendBoardState().Wait();
                            BroadcastMessage($"TURN:{currentPlayer}").Wait();
                            Console.WriteLine($"[TURN] Starting with Player {currentPlayer}");
                        }
                        else if (playerMap.Count < 2)
                        {
                            Console.WriteLine("[WAITING] Waiting for second player...");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting clients: {ex.Message}");
            }
        }

        private async Task HandleClient(TcpClient client, StreamReader reader, char playerSymbol)
        {
            try
            {
                // Changed: Remove gameActive from condition - we need to listen even before game starts
                while (client.Connected)
                {
                    string? message = await reader.ReadLineAsync();
                    if (message == null) break;

                    Console.WriteLine($"[RECV] Received from Player {playerSymbol}: {message}");

                    if (message.StartsWith("MOVE:"))
                    {
                        int cellIndex = int.Parse(message.Substring(5));
                        Console.WriteLine($"[PROCESSING] Processing move from Player {playerSymbol} at cell {cellIndex}");
                        await ProcessMove(cellIndex, playerSymbol);
                    }
                    else if (message == "RESTART")
                    {
                        Console.WriteLine($"[RESTART] Player {playerSymbol} requested game restart");
                        await RestartGame();
                    }
                    else if (message.StartsWith("NAME:"))
                    {
                        string playerName = message.Substring(5);
                        lock (lockObj)
                        {
                            playerNames[playerSymbol] = playerName;
                        }
                        Console.WriteLine($"[NAME] Player {playerSymbol} set name to: {playerName}");
                        
                        // Notify opponent about the name
                        char opponent = playerSymbol == 'X' ? 'Y' : 'X';
                        await SendMessageToPlayer(opponent, $"OPPONENT_NAME:{playerName}");
                    }
                    else if (message.StartsWith("CHAT:"))
                    {
                        // Format: CHAT:messageId:messageText
                        string chatData = message.Substring(5);
                        string messageId = "";
                        string messageText = "";
                        
                        int firstColon = chatData.IndexOf(':');
                        if (firstColon > 0)
                        {
                            messageId = chatData.Substring(0, firstColon);
                            messageText = chatData.Substring(firstColon + 1);
                        }
                        
                        string senderName = playerNames.ContainsKey(playerSymbol) ? playerNames[playerSymbol] : $"Player {playerSymbol}";
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        
                        Console.WriteLine($"[CHAT] {senderName}: {messageText}");
                        
                        // Route to opponent
                        char opponent = playerSymbol == 'X' ? 'Y' : 'X';
                        await SendMessageToPlayer(opponent, $"CHAT:{messageId}:{senderName}:{messageText}:{timestamp}");
                    }
                    else if (message.StartsWith("CALL_REQUEST"))
                    {
                        // Player wants to initiate video call
                        Console.WriteLine($"[VIDEO CALL] Player {playerSymbol} initiated call request");
                        char opponent = playerSymbol == 'X' ? 'Y' : 'X';
                        string callerName = playerNames.ContainsKey(playerSymbol) ? playerNames[playerSymbol] : $"Player {playerSymbol}";
                        await SendMessageToPlayer(opponent, $"CALL_REQUEST:{callerName}");
                    }
                    else if (message.StartsWith("CALL_ACCEPT"))
                    {
                        // Player accepted the call
                        Console.WriteLine($"[VIDEO CALL] Player {playerSymbol} accepted the call");
                        char opponent = playerSymbol == 'X' ? 'Y' : 'X';
                        await SendMessageToPlayer(opponent, "CALL_ACCEPT");
                    }
                    else if (message.StartsWith("CALL_REJECT"))
                    {
                        // Player rejected the call
                        Console.WriteLine($"[VIDEO CALL] Player {playerSymbol} rejected the call");
                        char opponent = playerSymbol == 'X' ? 'Y' : 'X';
                        await SendMessageToPlayer(opponent, "CALL_REJECT");
                    }
                    else if (message.StartsWith("CALL_END"))
                    {
                        // Player ended the call
                        Console.WriteLine($"[VIDEO CALL] Player {playerSymbol} ended the call");
                        char opponent = playerSymbol == 'X' ? 'Y' : 'X';
                        await SendMessageToPlayer(opponent, "CALL_END");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error handling Player {playerSymbol}: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                Console.WriteLine($"[DISCONNECT] Player {playerSymbol} handler loop exited");
                RemoveClient(client);
            }
        }

        private async Task ProcessMove(int cellIndex, char playerSymbol)
        {
            lock (lockObj)
            {
                if (!gameActive) return;

                if (playerSymbol != currentPlayer)
                {
                    SendMessageToPlayer(playerSymbol, "ERROR:Not your turn").Wait();
                    return;
                }

                if (cellIndex < 0 || cellIndex >= 9 || board[cellIndex] != ' ')
                {
                    SendMessageToPlayer(playerSymbol, "ERROR:Invalid move").Wait();
                    return;
                }

                board[cellIndex] = playerSymbol;
                Console.WriteLine($"[MOVE] Player {playerSymbol} moved to cell {cellIndex}");
            }

            await SendBoardState();

            char winner = CheckWinner();
            if (winner != ' ')
            {
                await BroadcastMessage($"WIN:{winner}");
                gameActive = false;
                Console.WriteLine($"[GAME OVER] Player {winner} wins!");
                return;
            }

            if (IsBoardFull())
            {
                await BroadcastMessage("DRAW");
                gameActive = false;
                Console.WriteLine("[GAME OVER] Game ended in a draw!");
                return;
            }

            lock (lockObj)
            {
                currentPlayer = currentPlayer == 'X' ? 'Y' : 'X';
            }
            await BroadcastMessage($"TURN:{currentPlayer}");
            Console.WriteLine($"[TURN] Switched to Player {currentPlayer}");
        }

        private char CheckWinner()
        {
            for (int i = 0; i < 3; i++)
            {
                if (board[i * 3] != ' ' &&
                    board[i * 3] == board[i * 3 + 1] &&
                    board[i * 3] == board[i * 3 + 2])
                    return board[i * 3];
            }

            for (int i = 0; i < 3; i++)
            {
                if (board[i] != ' ' &&
                    board[i] == board[i + 3] &&
                    board[i] == board[i + 6])
                    return board[i];
            }

            if (board[0] != ' ' && board[0] == board[4] && board[0] == board[8])
                return board[0];

            if (board[2] != ' ' && board[2] == board[4] && board[2] == board[6])
                return board[2];

            return ' ';
        }

        private bool IsBoardFull() => board.All(c => c != ' ');

        private async Task RestartGame()
        {
            lock (lockObj)
            {
                // Reset the board
                for (int i = 0; i < 9; i++) board[i] = ' ';
                
                // Reset to player X's turn
                currentPlayer = 'X';
                gameActive = true;
                
                Console.WriteLine("[RESTART] Game has been restarted. Board cleared.");
            }
            
            // Notify both players
            await BroadcastMessage("RESTART");
            await SendBoardState();
            await BroadcastMessage($"TURN:{currentPlayer}");
            Console.WriteLine($"[RESTART] New game started with Player {currentPlayer}");
        }

        private async Task SendBoardState()
        {
            string boardState = "BOARD:" + new string(board);
            await BroadcastMessage(boardState);
        }

        private async Task BroadcastMessage(string message)
        {
            foreach (var writer in writerMap.Values.ToList())
            {
                try { await writer.WriteLineAsync(message); }
                catch (Exception ex) { Console.WriteLine($"Error broadcasting: {ex.Message}"); }
            }
        }

        private async Task SendMessageToPlayer(char playerSymbol, string message)
        {
            if (writerMap.ContainsKey(playerSymbol))
            {
                try { await writerMap[playerSymbol].WriteLineAsync(message); }
                catch (Exception ex) { Console.WriteLine($"Error sending to player: {ex.Message}"); }
            }
        }

        private void RemoveClient(TcpClient client)
        {
            lock (lockObj)
            {
                // Find the player symbol for this client
                var playerEntry = playerMap.FirstOrDefault(x => x.Value == client);
                
                if (playerEntry.Key != default(char))
                {
                    char playerSymbol = playerEntry.Key;
                    Console.WriteLine($"[DEBUG] Removing player {playerSymbol} from maps");
                    
                    playerMap.Remove(playerSymbol);
                    writerMap.Remove(playerSymbol);
                    
                    Console.WriteLine($"[DEBUG] After removal - playerMap count: {playerMap.Count}");
                }

                if (gameActive && playerMap.Count < 2)
                {
                    gameActive = false;
                    Console.WriteLine("[GAME OVER] A player disconnected. Game ended.");
                    BroadcastMessage("ERROR:Player disconnected").Wait();
                }
            }
        }

        public void Stop()
        {
            listener?.Stop();
            foreach (var client in playerMap.Values)
                client.Close();

            playerMap.Clear();
            writerMap.Clear();
        }
    }
}
