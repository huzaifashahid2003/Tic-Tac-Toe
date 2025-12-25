# Tic Tac Toe - Multiplayer with Chat

A C# (.NET) multiplayer Tic Tac Toe game using **raw TCP socket programming** with chat functionality.

## 🎯 Features

### ✅ Multiplayer Gameplay
- **Two players** connect from different laptops over LAN/Wi-Fi
- Real-time turn-based game mechanics
- Win/Draw detection
- Game restart functionality

### 💬 Real-time Chat System
- **Player names** displayed in chat
- **Message timestamps** with format HH:mm:ss
- Server-routed messages (not peer-to-peer)
- Chat history preserved during game session
- Send messages via Enter key or Send button

### 📹 Video Call Feature
- **Webcam video streaming** between players during game
- Accept/Reject incoming video calls
- Real-time two-way video communication
- End call at any time
- Call duration timer
- Uses AForge.NET for webcam capture

### 🌐 LAN Connectivity
- Server displays **Local IP Address** on startup
- Clients can connect from different laptops on same Wi-Fi network
- Uses `System.Net.Sockets` (TCP)
- No SignalR, WebSockets, or WebRTC

---

## 🏗️ Architecture

```
┌─────────────────┐          ┌──────────────────┐          ┌─────────────────┐
│  Client 1 (WPF) │◄────────►│  Server (TCP)    │◄────────►│  Client 2 (WPF) │
│  Laptop A       │          │  Console App     │          │  Laptop B       │
└─────────────────┘          └──────────────────┘          └─────────────────┘
   - Player Name                - Turn Logic                  - Player Name
   - Game Board                 - Game State                  - Game Board
   - Chat Panel                 - Message Routing             - Chat Panel
```

### Server Responsibilities
- Accept 2 TCP client connections
- Manage game state (board, turns)
- Validate moves
- Route chat messages between players
- Track player names
- Route video call signaling (request, accept, reject, end)

### Client Responsibilities
- Connect to server via IP:Port
- Display game board and status
- Send moves and chat messages
- Receive updates from server
- Show chat history with timestamps
- Manage video call (webcam capture, streaming, display)

---

## 📡 Protocol Messages

### Connection & Identity
- `PLAYER:X|Y` - Assign player symbol
- `NAME:playerName` - Set player name
- `OPPONENT_NAME:name` - Notify opponent's name

### Game Messages
- `START` - Game started
- `BOARD:XXXXXXXXX` - Board state (9 chars)
- `TURN:X|Y` - Current turn
- `MOVE:0-8` - Player move (cell index)
- `WIN:X|Y` - Winner announcement
- `DRAW` - Draw result
- `RESTART` - New game request

### Chat Messages
- `CHAT:messageId:messageText` - Send chat (client→server)
- `CHAT:messageId:sender:text:timestamp` - Receive chat (server→client)

### Video Call Messages
- `CALL_REQUEST` - Initiate video call (client→server→opponent)
- `CALL_REQUEST:callerName` - Incoming call notification (server→client)
- `CALL_ACCEPT` - Accept video call (client→server→opponent)
- `CALL_REJECT` - Reject video call (client→server→opponent)
- `CALL_END` - End video call (client→server→opponent)

### Error Messages
- `ERROR:message` - Error notification

---

## 🚀 Setup Instructions

### Step 1: Find Your LAN IP Address

#### On Windows:
```powershell
ipconfig
```
Look for **IPv4 Address** under your active Wi-Fi adapter (e.g., `192.168.1.100`)

#### On macOS/Linux:
```bash
ifconfig
```
Look for **inet** address (e.g., `192.168.1.100`)

### Step 2: Start the Server

1. Close any running instances (if rebuilding)
2. Open terminal in `TicTacToeServer` folder
3. Run:
```bash
cd TicTacToeServer
dotnet build
dotnet run
```

**Server Output:**
```
===========================================
  SERVER STARTED ON PORT 5000
===========================================
Local IP Address: 192.168.1.100
Clients should connect to: 192.168.1.100:5000
===========================================
Waiting for players...
```

📝 **Note the IP address** - clients will need this!

### Step 3: Start Client 1 (Laptop A)

1. Open terminal in `TicTacToeClient` folder
2. Run:
```bash
cd TicTacToeClient
dotnet build
dotnet run
```

3. In the client window:
   - **Your Name:** Enter your name (e.g., "Alice")
   - **Server IP:** Enter server IP (e.g., `192.168.1.100`)
   - **Port:** `5000`
   - Click **Connect**

### Step 4: Start Client 2 (Laptop B)

Repeat Step 3 on the second laptop with different name (e.g., "Bob")

---

## 🎮 How to Play

### Starting the Game
1. Both players connect to the server
2. Game starts automatically when both players are connected
3. Player X goes first

### Making Moves
1. Wait for your turn (displays "🎮 Your turn!")
2. Click any empty cell on the board
3. Your move is sent to server and opponent

### Chatting
1. Type message in chat input box
2. Press **Enter** or click **Send**
3. Messages appear with timestamp [HH:mm:ss]
4. Chat history preserved during game session

### Video Calling
1. Click **📹 Video Call** button
2. Opponent receives popup: "Accept" or "Reject"
3. **If accepted:**
   - Receiver's video window opens and starts listening
   - Receiver shares their video port number with caller
   - Caller enters the port and connects
   - Both webcams start streaming
   - Video call window shows:
     - **Left panel:** Your camera
     - **Right panel:** Opponent's camera
     - Call duration timer
4. **If rejected:**
   - Caller receives notification
5. **To end call:**
   - Click **End Call** button
   - Or close video window

**Note:** Video streaming requires:
- Webcam/camera on both laptops
- Firewall allowing TCP connections on random ports
- Both players on same LAN/Wi-Fi network

### Winning/Draw
- Game ends when someone gets 3 in a row or board is full
- Click **New Game (Reconnect)** to restart

---

## 🛠️ Technical Details

### Technologies
- **.NET 8.0**
- **WPF** (Windows Presentation Foundation) for client UI
- **System.Net.Sockets.TcpClient** / **TcpListener**
- **Async/await** for non-blocking I/O
- **ObservableCollection** for chat messages
- **AForge.NET** for webcam capture
- **System.Drawing** for image processing

### Project Structure
```
NP_Project/
├── TicTacToeServer/
│   ├── Program.cs           # Entry point
│   ├── GameServer.cs        # TCP server logic
│   └── TicTacToeServer.csproj
│
├── TicTacToeClient/
│   ├── MainWindow.xaml         # Main UI layout
│   ├── MainWindow.xaml.cs      # Main UI logic
│   ├── VideoCallWindow.xaml    # Video call UI
│   ├── VideoCallWindow.xaml.cs # Video call logic
│   ├── Models/
│   │   └── GameState.cs        # Game state & chat messages
│   ├── Services/
│   │   ├── NetworkManager.cs      # TCP client wrapper
│   │   ├── GameController.cs      # Game logic handler
│   │   └── VideoCallManager.cs    # Video streaming manager
│   └── TicTacToeClient.csproj
│
└── README.md
```

### Key Classes

#### Server: `GameServer.cs`
- `Start()` - Initialize and bind to port
- `AcceptClients()` - Handle incoming connections
- `HandleClient()` - Process client messages
- `ProcessMove()` - Validate and execute moves
- `BroadcastMessage()` - Send to all clients

#### Client: `NetworkManager.cs`
- `ConnectAsync()` - Connect to server
- `SendMoveAsync()` - Send game move
- `SendChatAsync()` - Send chat message
- `SendNameAsync()` - Send player name
- `SendSeenAsync()` - Mark message as seen

#### Client: `GameController.cs`
- `ProcessServerMessage()` - Parse server messages
- `AddMyChatMessage()` - Add outgoing chat
- State management and event notifications

#### Client: `GameState.cs`
- Board state (char[9])
- Player names
- Chat messages (ObservableCollection)
- Turn tracking

---

## 🐛 Troubleshooting

### "Connection failed"
- Verify server is running
- Check firewall settings (allow port 5000)
- Ensure both devices on same Wi-Fi network
- Use correct server IP address (not 127.0.0.1 for remote clients)

### "Build failed - file locked"
- Close running server/client instances
- Run `dotnet clean` then `dotnet build`

### Client doesn't receive messages
- Check server console for error messages
- Verify network connectivity
- Try restarting both server and clients

### Chat not working
- Ensure both players are connected
- Check that names are entered before connecting
- Look for error messages in server console

---

## 📋 Testing Checklist

- [ ] Server displays local IP on startup
- [ ] Two clients can connect from different laptops
- [ ] Player names appear in chat
- [ ] Game turns alternate correctly
- [ ] Chat messages appear with timestamps
- [ ] Seen status (✓✓) updates correctly
- [ ] Win detection works
- [ ] Draw detection works
- [ ] Game restart functionality works
- [ ] Disconnect handling works

---

## 🎓 Academic Project Requirements

✅ **Raw TCP Socket Programming**
- Uses `System.Net.Sockets.TcpClient` and `TcpListener`
- No high-level frameworks (SignalR/WebSockets/WebRTC)

✅ **Client-Server Architecture**
- Centralized server manages state
- Clients communicate only through server
- Server validates all moves

✅ **Multiple Laptop Connections**
- Server binds to LAN IP (not localhost)
- Tested on different devices
- Wi-Fi network communication

✅ **Chat Feature**
- Real-time messaging
- Server-routed (not P2P)
- Sender name, message, timestamp
- Seen status tracking

---

## 📚 Extended Protocol Documentation

### Message Flow Examples

#### Player Connection:
```
Client → Server: [TCP Connect]
Server → Client: PLAYER:X
Client → Server: NAME:Alice
Server → Other: OPPONENT_NAME:Alice
```

#### Making a Move:
```
Client X → Server: MOVE:4
Server → Both: BOARD:    X    
Server → Both: TURN:Y
```

#### Chat Message:
```
Alice → Server: CHAT:abc123:Hello!
Server → Bob: CHAT:abc123:Alice:Hello!:2025-12-25 14:30:45
Bob → Server: SEEN:abc123
Server → Alice: SEEN:abc123
```

---

## 🔒 Security Note

⚠️ This is an **educational project**. For production use, consider:
- Authentication
- Encryption (TLS/SSL)
- Input validation
- Rate limiting
- Error handling
- Logging

---

## 📝 License

Academic Project - Network Programming Course

---

## 🤝 Contributing

This is an academic project. Feel free to fork and improve!

---

## 📧 Support

For issues or questions about the project, refer to the code comments and this README.

---

**Happy Gaming! 🎮**
