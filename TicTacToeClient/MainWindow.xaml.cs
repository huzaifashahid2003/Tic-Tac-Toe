using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TicTacToeClient.Models;
using TicTacToeClient.Services;

namespace TicTacToeClient
{
    /// <summary>
    /// Main window with improved modular architecture and state management
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly NetworkManager networkManager;
        private readonly GameController gameController;
        private readonly Button[] cellButtons = new Button[9];
        private VideoCallManager? videoCallManager;
        private VideoCallWindow? videoCallWindow;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize services
            networkManager = new NetworkManager();
            gameController = new GameController();
            
            // Subscribe to events
            SetupEventHandlers();
            
            // Initialize UI
            InitializeCellButtons();
            UpdateUI();
        }

        /// <summary>
        /// Setup all event handlers for network and game events
        /// </summary>
        private void SetupEventHandlers()
        {
            networkManager.MessageReceived += OnMessageReceived;
            networkManager.ErrorOccurred += OnErrorOccurred;
            networkManager.ConnectionLost += OnConnectionLost;
            gameController.StateChanged += OnStateChanged;
            gameController.NewChatMessage += OnNewChatMessage;
            gameController.IncomingCall += OnIncomingCall;
            gameController.CallAccepted += OnCallAccepted;
            gameController.CallInfo += OnCallInfo;
            gameController.CallRejected += OnCallRejected;
            gameController.CallEnded += OnCallEnded;
        }

        private void InitializeCellButtons()
        {
            cellButtons[0] = btn0;
            cellButtons[1] = btn1;
            cellButtons[2] = btn2;
            cellButtons[3] = btn3;
            cellButtons[4] = btn4;
            cellButtons[5] = btn5;
            cellButtons[6] = btn6;
            cellButtons[7] = btn7;
            cellButtons[8] = btn8;
        }

        #region Event Handlers

        /// <summary>
        /// Handle incoming messages from server
        /// </summary>
        private void OnMessageReceived(string message)
        {
            Dispatcher.Invoke(() =>
            {
                gameController.ProcessServerMessage(message);
            });
        }

        /// <summary>
        /// Handle network errors
        /// </summary>
        private void OnErrorOccurred(string error)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        /// <summary>
        /// Handle connection loss
        /// </summary>
        private void OnConnectionLost()
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Connection to server lost.", "Disconnected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ResetGame();
            });
        }

        /// <summary>
        /// Handle game state changes and update UI
        /// </summary>
        private void OnStateChanged()
        {
            UpdateUI();
        }

        /// <summary>
        /// Handle new chat message - auto scroll
        /// </summary>
        private void OnNewChatMessage(ChatMessage message)
        {
            Dispatcher.Invoke(() =>
            {
                // Scroll to bottom
                if (lstChat.Items.Count > 0)
                {
                    lstChat.ScrollIntoView(lstChat.Items[lstChat.Items.Count - 1]);
                }
            });
        }

        #endregion

        #region Button Click Handlers

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtPort.Text, out int port))
            {
                MessageBox.Show("Please enter a valid port number.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string serverIP = txtServerIP.Text.Trim();
            if (string.IsNullOrEmpty(serverIP))
            {
                MessageBox.Show("Please enter a server IP address.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string playerName = txtPlayerName.Text.Trim();
            if (string.IsNullOrEmpty(playerName))
            {
                MessageBox.Show("Please enter your name.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnConnect.IsEnabled = false;
            txtStatus.Text = "Connecting...";
            txtStatus.Foreground = Brushes.Orange;

            bool connected = await networkManager.ConnectAsync(serverIP, port);

            if (connected)
            {
                // Store player name in game state
                gameController.State.PlayerName = playerName;
                
                // Send player name to server
                await networkManager.SendNameAsync(playerName);
                
                txtStatus.Text = "Connected! Waiting for opponent...";
                txtStatus.Foreground = Brushes.Blue;
                txtServerIP.IsEnabled = false;
                txtPort.IsEnabled = false;
                txtPlayerName.IsEnabled = false;
                
                // Enable chat and video call
                txtChatInput.IsEnabled = true;
                btnSendChat.IsEnabled = true;
                btnVideoCall.IsEnabled = true;
            }
            else
            {
                btnConnect.IsEnabled = true;
                txtStatus.Text = "Connection failed";
                txtStatus.Foreground = Brushes.Red;
            }
        }

        private async void Cell_Click(object sender, RoutedEventArgs e)
        {
            GameState state = gameController.State;

            System.Diagnostics.Debug.WriteLine($"[CLICK] Cell clicked. IsMyTurn: {state.IsMyTurn}");
            System.Diagnostics.Debug.WriteLine($"[CLICK] MySymbol: {state.MySymbol}, CurrentTurn: {state.CurrentTurn}");
            System.Diagnostics.Debug.WriteLine($"[CLICK] IsGameActive: {state.IsGameActive}");
            
            if (!state.IsMyTurn)
            {
                System.Diagnostics.Debug.WriteLine("[CLICK] Not my turn - exiting");
                return;
            }

            Button clickedButton = (Button)sender;
            int cellIndex = int.Parse(clickedButton.Tag.ToString()!);

            System.Diagnostics.Debug.WriteLine($"[CLICK] Clicked cell index: {cellIndex}");
            System.Diagnostics.Debug.WriteLine($"[CLICK] Cell content: '{state.Board[cellIndex]}'");

            // Check if cell is already occupied
            if (state.Board[cellIndex] != ' ')
            {
                System.Diagnostics.Debug.WriteLine($"[CLICK] Cell already occupied - exiting");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[CLICK] Sending move to server...");
            bool sent = await networkManager.SendMoveAsync(cellIndex);
            System.Diagnostics.Debug.WriteLine($"[CLICK] Move sent result: {sent}");
        }

        private async void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (!networkManager.IsConnected)
            {
                MessageBox.Show("Not connected to server.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Start a new game?", "New Game", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                btnReset.IsEnabled = false;
                bool sent = await networkManager.SendRestartAsync();
                
                if (!sent)
                {
                    MessageBox.Show("Failed to restart game.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    btnReset.IsEnabled = true;
                }
            }
        }

        private async void BtnSendChat_Click(object sender, RoutedEventArgs e)
        {
            await SendChatMessage();
        }

        private async void TxtChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SendChatMessage();
            }
        }

        private async System.Threading.Tasks.Task SendChatMessage()
        {
            string message = txtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            if (!networkManager.IsConnected)
            {
                MessageBox.Show("Not connected to server.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string messageId = Guid.NewGuid().ToString();
            
            // Add to local chat
            gameController.AddMyChatMessage(messageId, message);
            
            // Send to server
            await networkManager.SendChatAsync(messageId, message);
            
            // Clear input
            txtChatInput.Clear();
        }

        #endregion

        #region UI Update Methods

        /// <summary>
        /// Central method to update all UI elements based on game state
        /// </summary>
        private void UpdateUI()
        {
            GameState state = gameController.State;

            // Update status display
            txtStatus.Text = state.StatusMessage;
            txtTurn.Text = state.TurnMessage;

            // Update status colors
            if (state.StatusMessage.Contains("WIN"))
            {
                txtStatus.Foreground = Brushes.Green;
            }
            else if (state.StatusMessage.Contains("wins") || state.StatusMessage.Contains("Error"))
            {
                txtStatus.Foreground = Brushes.Red;
            }
            else if (state.StatusMessage.Contains("DRAW"))
            {
                txtStatus.Foreground = Brushes.Orange;
            }
            else if (state.IsConnected)
            {
                txtStatus.Foreground = Brushes.Blue;
            }
            else
            {
                txtStatus.Foreground = Brushes.Black;
            }

            // Update turn display colors
            if (state.IsMyTurn)
            {
                txtTurn.Foreground = Brushes.Green;
                txtTurn.FontWeight = FontWeights.Bold;
            }
            else
            {
                txtTurn.Foreground = Brushes.Gray;
                txtTurn.FontWeight = FontWeights.Normal;
            }

            // Update board
            UpdateBoardDisplay(state);

            // Update button states - enable when connected and game is over
            btnReset.IsEnabled = state.IsConnected && !state.IsGameActive;
            
            // Update chat display
            UpdateChatDisplay(state);
        }

        /// <summary>
        /// Update simple chat display
        /// </summary>
        private void UpdateChatDisplay(GameState state)
        {
            lstChat.Items.Clear();
            
            foreach (var msg in state.ChatMessages)
            {
                string displayText = $"[{msg.FormattedTime}] {msg.Sender}: {msg.Message}";
                lstChat.Items.Add(displayText);
            }
            
            // Auto scroll to bottom
            if (lstChat.Items.Count > 0)
            {
                lstChat.ScrollIntoView(lstChat.Items[lstChat.Items.Count - 1]);
            }
        }

        /// <summary>
        /// Update the board display based on game state
        /// </summary>
        private void UpdateBoardDisplay(GameState state)
        {
            for (int i = 0; i < 9; i++)
            {
                char cell = state.Board[i];
                
                if (cell == 'X')
                {
                    cellButtons[i].Content = "X";
                    cellButtons[i].Foreground = Brushes.Blue;
                    cellButtons[i].IsEnabled = false;
                }
                else if (cell == 'Y')
                {
                    cellButtons[i].Content = "Y";
                    cellButtons[i].Foreground = Brushes.Red;
                    cellButtons[i].IsEnabled = false;
                }
                else
                {
                    cellButtons[i].Content = "";
                    cellButtons[i].Foreground = Brushes.Black;
                    cellButtons[i].IsEnabled = state.IsGameActive && state.IsMyTurn;
                }
            }
        }

        /// <summary>
        /// Reset the game and disconnect
        /// </summary>
        private void ResetGame()
        {
            // Disconnect from server
            networkManager.Disconnect();
            
            // Reset game state
            gameController.ResetGame();

            // Reset UI controls
            foreach (var button in cellButtons)
            {
                button.Content = "";
                button.IsEnabled = false;
                button.Foreground = Brushes.Black;
            }

            txtStatus.Text = "Not connected";
            txtStatus.Foreground = Brushes.Black;
            txtTurn.Text = "";
            btnConnect.IsEnabled = true;
            btnReset.IsEnabled = false;
            txtServerIP.IsEnabled = true;
            txtPort.IsEnabled = true;
            txtPlayerName.IsEnabled = true;
            
            // Reset chat
            txtChatInput.IsEnabled = false;
            btnSendChat.IsEnabled = false;
            lstChat.Items.Clear();

            // Reset video call
            btnVideoCall.IsEnabled = false;
        }

        #endregion

        #region Video Call Methods

        private void OnIncomingCall(string callerName)
        {
            Dispatcher.Invoke(() =>
            {
                var result = MessageBox.Show(
                    $"{callerName} is calling you.\n\nDo you want to accept the video call?",
                    "Incoming Video Call",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _ = AcceptVideoCall();
                }
                else
                {
                    _ = RejectVideoCall();
                }
            });
        }

        private void OnCallAccepted()
        {
            Dispatcher.Invoke(() =>
            {
                // Call was accepted - just wait for connection info
                txtStatus.Text = "Call accepted! Waiting for connection...";
            });
        }

        private void OnCallInfo(string opponentIP, int opponentPort)
        {
            Dispatcher.Invoke(async () =>
            {
                // Received opponent's IP and port - connect automatically
                await StartVideoCallAsInitiator(opponentIP, opponentPort);
            });
        }

        private void OnCallRejected()
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Call was rejected.", "Video Call", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private void OnCallEnded()
        {
            Dispatcher.Invoke(() =>
            {
                // Close video window if open
                if (videoCallWindow != null)
                {
                    try
                    {
                        videoCallWindow.Close();
                    }
                    catch { }
                    videoCallWindow = null;
                }
                
                MessageBox.Show("The call has ended.", "Video Call", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private async void BtnVideoCall_Click(object sender, RoutedEventArgs e)
        {
            // Initiate video call request
            btnVideoCall.IsEnabled = false;
            
            bool sent = await networkManager.SendCallRequestAsync();
            
            if (!sent)
            {
                MessageBox.Show("Failed to send call request.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                btnVideoCall.IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task AcceptVideoCall()
        {
            try
            {
                // Send accept message
                await networkManager.SendCallAcceptAsync();
                
                // Create video manager
                videoCallManager = new VideoCallManager();
                
                // Start listening for incoming video stream
                bool listening = await videoCallManager.StartListeningAsync();
                
                if (!listening)
                {
                    MessageBox.Show("Failed to start video listener.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Send our listening port to server
                await networkManager.SendCallInfoAsync(videoCallManager.StreamPort);

                System.Diagnostics.Debug.WriteLine($"[CALL] Accepting call - listening on port {videoCallManager.StreamPort}");

                // Create and show video call window
                videoCallWindow = new VideoCallWindow(videoCallManager, networkManager);
                videoCallWindow.Show();
                videoCallWindow.StartCall();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accepting call: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task RejectVideoCall()
        {
            // Send reject message
            await networkManager.SendCallRejectAsync();
        }

        private async System.Threading.Tasks.Task StartVideoCallAsInitiator(string opponentIP, int opponentPort)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CALL] Initiating call - connecting to {opponentIP}:{opponentPort}");
                
                // Create video manager
                videoCallManager = new VideoCallManager();
                
                // Connect to opponent's video stream using provided IP and port
                bool connected = await videoCallManager.ConnectToStreamAsync(opponentIP, opponentPort);
                
                if (!connected)
                {
                    MessageBox.Show("Failed to connect to video stream.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    btnVideoCall.IsEnabled = true;
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[CALL] Connected successfully!");

                // Create and show video call window
                videoCallWindow = new VideoCallWindow(videoCallManager, networkManager);
                videoCallWindow.Show();
                videoCallWindow.StartCall();
                
                btnVideoCall.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting video call: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                btnVideoCall.IsEnabled = true;
            }
        }

        #endregion

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            ResetGame();
            base.OnClosing(e);
        }
    }
}
