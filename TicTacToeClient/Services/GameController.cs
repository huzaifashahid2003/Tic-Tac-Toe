using System;
using System.Linq;
using TicTacToeClient.Models;

namespace TicTacToeClient.Services
{
    /// <summary>
    /// Handles game logic and state updates
    /// </summary>
    public class GameController
    {
        private readonly GameState gameState;

        public event Action? StateChanged;
        public event Action<ChatMessage>? NewChatMessage; // Event for new chat messages
        public event Action<string>? IncomingCall; // Event for incoming video call (caller name)
        public event Action? CallAccepted; // Event when call is accepted
        public event Action<string, int>? CallInfo; // Event with opponent IP and port
        public event Action? CallRejected; // Event when call is rejected
        public event Action? CallEnded; // Event when call ends

        public GameState State => gameState;

        public GameController()
        {
            gameState = new GameState();
        }

        /// <summary>
        /// Processes messages received from the server
        /// </summary>
        public void ProcessServerMessage(string message)
        {
            if (message.StartsWith("PLAYER:"))
            {
                gameState.MySymbol = message[7];
                gameState.StatusMessage = $"You are {gameState.GetPlayerIdentity()}";
                gameState.IsConnected = true;
                StateChanged?.Invoke();
            }
            else if (message == "START")
            {
                gameState.IsGameActive = true;
                gameState.StatusMessage = $"Game started! You are {gameState.GetPlayerIdentity()}";
                StateChanged?.Invoke();
            }
            else if (message == "RESTART")
            {
                gameState.ResetBoard();
                gameState.IsGameActive = true;
                gameState.StatusMessage = $"New game started! You are {gameState.GetPlayerIdentity()}";
                gameState.TurnMessage = "";
                StateChanged?.Invoke();
            }
            else if (message.StartsWith("BOARD:"))
            {
                string boardState = message.Substring(6);
                UpdateBoard(boardState);
                StateChanged?.Invoke();
            }
            else if (message.StartsWith("TURN:"))
            {
                gameState.CurrentTurn = message[5];
                UpdateTurnMessage();
                StateChanged?.Invoke();
            }
            else if (message.StartsWith("WIN:"))
            {
                char winner = message[4];
                gameState.IsGameActive = false;
                
                if (winner == gameState.MySymbol)
                {
                    gameState.StatusMessage = "🎉 You WIN! 🎉";
                }
                else
                {
                    gameState.StatusMessage = $"Player {winner} wins!";
                }
                gameState.TurnMessage = "Game Over";
                StateChanged?.Invoke();
            }
            else if (message == "DRAW")
            {
                gameState.IsGameActive = false;
                gameState.StatusMessage = "Game ended in a DRAW!";
                gameState.TurnMessage = "Game Over";
                StateChanged?.Invoke();
            }
            else if (message.StartsWith("ERROR:"))
            {
                string error = message.Substring(6);
                gameState.StatusMessage = $"Error: {error}";
                StateChanged?.Invoke();
            }
            else if (message.StartsWith("OPPONENT_NAME:"))
            {
                gameState.OpponentName = message.Substring(14);
                gameState.StatusMessage = $"Playing against {gameState.OpponentName}";
                StateChanged?.Invoke();
            }
            else if (message.StartsWith("CHAT:"))
            {
                // Format: CHAT:messageId:sender:messageText:timestamp
                ProcessChatMessage(message.Substring(5));
            }
            else if (message.StartsWith("CALL_REQUEST:"))
            {
                // Incoming video call request
                string callerName = message.Substring(13);
                IncomingCall?.Invoke(callerName);
            }
            else if (message == "CALL_ACCEPT")
            {
                // Opponent accepted the call
                CallAccepted?.Invoke();
            }
            else if (message.StartsWith("CALL_INFO:"))
            {
                // Received opponent's connection info
                string info = message.Substring(10);
                string[] parts = info.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out int port))
                {
                    CallInfo?.Invoke(parts[0], port);
                }
            }
            else if (message == "CALL_REJECT")
            {
                // Opponent rejected the call
                CallRejected?.Invoke();
            }
            else if (message == "CALL_END")
            {
                // Opponent ended the call
                CallEnded?.Invoke();
            }

        }

        private void ProcessChatMessage(string chatData)
        {
            try
            {
                var parts = chatData.Split(new[] { ':' }, 4);
                if (parts.Length >= 4)
                {
                    string messageId = parts[0];
                    string sender = parts[1];
                    string messageText = parts[2];
                    DateTime timestamp = DateTime.Parse(parts[3]);

                    var chatMessage = new ChatMessage
                    {
                        Id = messageId,
                        Sender = sender,
                        Message = messageText,
                        Timestamp = timestamp,
                        IsMine = false
                    };

                    gameState.ChatMessages.Add(chatMessage);
                    NewChatMessage?.Invoke(chatMessage);
                    StateChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing chat message: {ex.Message}");
            }
        }

        public void AddMyChatMessage(string messageId, string messageText)
        {
            var chatMessage = new ChatMessage
            {
                Id = messageId,
                Sender = gameState.PlayerName,
                Message = messageText,
                Timestamp = DateTime.Now,
                IsMine = true
            };

            gameState.ChatMessages.Add(chatMessage);
            StateChanged?.Invoke();
        }

        private void UpdateBoard(string boardState)
        {
            for (int i = 0; i < 9 && i < boardState.Length; i++)
            {
                gameState.Board[i] = boardState[i];
            }
        }

        private void UpdateTurnMessage()
        {
            if (gameState.IsMyTurn)
            {
                gameState.TurnMessage = "🎮 Your turn!";
            }
            else if (gameState.IsGameActive)
            {
                gameState.TurnMessage = $"⏳ Waiting for Player {gameState.CurrentTurn}...";
            }
            else
            {
                gameState.TurnMessage = "";
            }
        }

        public void ResetGame()
        {
            gameState.MySymbol = ' ';
            gameState.CurrentTurn = ' ';
            gameState.IsGameActive = false;
            gameState.IsConnected = false;
            gameState.StatusMessage = "Not connected";
            gameState.TurnMessage = "";
            gameState.ResetBoard();
            StateChanged?.Invoke();
        }
    }
}
