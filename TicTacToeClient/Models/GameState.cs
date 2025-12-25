using System;
using System.Collections.ObjectModel;

namespace TicTacToeClient.Models
{
    /// <summary>
    /// Represents the current state of the game
    /// </summary>
    public class GameState
    {
        public char MySymbol { get; set; } = ' ';
        public char CurrentTurn { get; set; } = ' ';
        public bool IsGameActive { get; set; } = false;
        public bool IsConnected { get; set; } = false;
        public char[] Board { get; set; } = new char[9];
        public string StatusMessage { get; set; } = "Not connected";
        public string TurnMessage { get; set; } = "";
        
        // Player names
        public string PlayerName { get; set; } = "";
        public string OpponentName { get; set; } = "";
        
        // Chat messages
        public ObservableCollection<ChatMessage> ChatMessages { get; set; } = new ObservableCollection<ChatMessage>();

        public GameState()
        {
            ResetBoard();
        }

        public void ResetBoard()
        {
            for (int i = 0; i < 9; i++)
            {
                Board[i] = ' ';
            }
        }

        public bool IsMyTurn => IsGameActive && CurrentTurn == MySymbol;

        public string GetPlayerIdentity()
        {
            if (!string.IsNullOrEmpty(PlayerName))
                return $"{PlayerName} (Player {MySymbol})";
            return MySymbol != ' ' ? $"Player {MySymbol}" : "Unknown";
        }
    }

    /// <summary>
    /// Represents a chat message
    /// </summary>
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Sender { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsMine { get; set; } = false;
        public bool IsSeen { get; set; } = false;

        public string FormattedTime => Timestamp.ToString("HH:mm:ss");
        public string SeenStatus => IsSeen ? "✓✓" : "✓";
    }
}
