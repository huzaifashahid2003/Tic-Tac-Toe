# System Architecture Diagram

```
╔════════════════════════════════════════════════════════════════════════════╗
║                         TIC TAC TOE MULTIPLAYER SYSTEM                     ║
║                        with Real-Time Chat Feature                         ║
╚════════════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────────────┐
│                              NETWORK TOPOLOGY                                │
└─────────────────────────────────────────────────────────────────────────────┘

                         ┌──────────────────────┐
                         │   Wi-Fi Router       │
                         │   192.168.1.1        │
                         └──────────┬───────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
         ┌──────────▼──────────┐         ┌─────────▼──────────┐
         │   LAPTOP A          │         │   LAPTOP B         │
         │   192.168.1.100     │         │   192.168.1.101    │
         │                     │         │                    │
         │   ┌─────────────┐   │         │  ┌─────────────┐  │
         │   │   SERVER    │   │         │  │   CLIENT    │  │
         │   │   :5000     │   │         │  │             │  │
         │   └──────┬──────┘   │         │  └──────┬──────┘  │
         │          │          │         │         │         │
         │   ┌──────▼──────┐   │         │         │         │
         │   │   CLIENT    │   │         │         │         │
         │   │   Alice     │◄──┼─────────┼─────────┼─────────┤
         │   └─────────────┘   │   TCP   │    Bob           │
         └─────────────────────┘         └────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                           COMPONENT ARCHITECTURE                             │
└─────────────────────────────────────────────────────────────────────────────┘

SERVER (Console App)
┌─────────────────────────────────────────────────────────────┐
│ TicTacToeServer                                             │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Program.cs                                              │ │
│ │  • Entry point                                          │ │
│ │  • Initialize GameServer                                │ │
│ │  • Handle console input                                 │ │
│ └─────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ GameServer.cs                                           │ │
│ │                                                         │ │
│ │  Network Layer:                                         │ │
│ │   ├─ TcpListener (port 5000)                           │ │
│ │   ├─ AcceptClients() - async accept loop               │ │
│ │   ├─ HandleClient() - per-client message loop          │ │
│ │   └─ GetLocalIPAddress() - LAN IP detection            │ │
│ │                                                         │ │
│ │  Game State:                                            │ │
│ │   ├─ board[9] - game board                             │ │
│ │   ├─ currentPlayer - whose turn                        │ │
│ │   ├─ playerMap - client connections                    │ │
│ │   ├─ playerNames - player display names                │ │
│ │   └─ gameActive - game status                          │ │
│ │                                                         │ │
│ │  Game Logic:                                            │ │
│ │   ├─ ProcessMove() - validate & execute moves          │ │
│ │   ├─ CheckWinner() - detect win conditions             │ │
│ │   ├─ IsBoardFull() - detect draw                       │ │
│ │   └─ RestartGame() - reset for new game                │ │
│ │                                                         │ │
│ │  Chat System:                                           │ │
│ │   ├─ Handle NAME messages                              │ │
│ │   ├─ Route CHAT messages                               │ │
│ │   ├─ Add timestamps                                    │ │
│ │   └─ Handle SEEN status                                │ │
│ │                                                         │ │
│ │  Broadcasting:                                          │ │
│ │   ├─ BroadcastMessage() - send to all                  │ │
│ │   ├─ SendBoardState() - sync game board                │ │
│ │   └─ SendMessageToPlayer() - send to one               │ │
│ └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘


CLIENT (WPF App)
┌─────────────────────────────────────────────────────────────┐
│ TicTacToeClient                                             │
│                                                             │
│ ┌───────────────────────────────────────────────────────┐   │
│ │ MainWindow.xaml.cs (View Layer)                       │   │
│ │  ├─ Connection UI controls                            │   │
│ │  ├─ Game board buttons                                │   │
│ │  ├─ Chat UI components                                │   │
│ │  ├─ Event handlers                                    │   │
│ │  └─ UI update methods                                 │   │
│ └───────────────────────────────────────────────────────┘   │
│                      ▲        │                              │
│                      │        ▼                              │
│ ┌───────────────────────────────────────────────────────┐   │
│ │ Services/                                             │   │
│ │                                                       │   │
│ │ ┌─────────────────────────────────────────────────┐   │   │
│ │ │ NetworkManager.cs                               │   │   │
│ │ │  Network Communication:                         │   │   │
│ │ │   ├─ TcpClient connection                       │   │   │
│ │ │   ├─ StreamReader/Writer                        │   │   │
│ │ │   ├─ ConnectAsync() - establish connection      │   │   │
│ │ │   ├─ ListenToServerAsync() - receive loop       │   │   │
│ │ │   ├─ SendMoveAsync() - send moves               │   │   │
│ │ │   ├─ SendChatAsync() - send chat                │   │   │
│ │ │   ├─ SendNameAsync() - send player name         │   │   │
│ │ │   ├─ SendSeenAsync() - mark seen                │   │   │
│ │ │   └─ SendRestartAsync() - request restart       │   │   │
│ │ │  Events:                                        │   │   │
│ │ │   ├─ MessageReceived                            │   │   │
│ │ │   ├─ ErrorOccurred                              │   │   │
│ │ │   └─ ConnectionLost                             │   │   │
│ │ └─────────────────────────────────────────────────┘   │   │
│ │                                                       │   │
│ │ ┌─────────────────────────────────────────────────┐   │   │
│ │ │ GameController.cs                               │   │   │
│ │ │  Message Processing:                            │   │   │
│ │ │   ├─ ProcessServerMessage() - parse protocol    │   │   │
│ │ │   ├─ Handle PLAYER assignment                   │   │   │
│ │ │   ├─ Handle BOARD updates                       │   │   │
│ │ │   ├─ Handle TURN changes                        │   │   │
│ │ │   ├─ Handle WIN/DRAW                            │   │   │
│ │ │   ├─ ProcessChatMessage() - parse chat          │   │   │
│ │ │   └─ MarkMessageAsSeen() - update seen status   │   │   │
│ │ │  Events:                                        │   │   │
│ │ │   ├─ StateChanged                               │   │   │
│ │ │   └─ NewChatMessage                             │   │   │
│ │ └─────────────────────────────────────────────────┘   │   │
│ └───────────────────────────────────────────────────────┘   │
│                      ▲        │                              │
│                      │        ▼                              │
│ ┌───────────────────────────────────────────────────────┐   │
│ │ Models/                                               │   │
│ │                                                       │   │
│ │ ┌─────────────────────────────────────────────────┐   │   │
│ │ │ GameState.cs                                    │   │   │
│ │ │  Game Data:                                     │   │   │
│ │ │   ├─ MySymbol (X/Y)                             │   │   │
│ │ │   ├─ CurrentTurn (X/Y)                          │   │   │
│ │ │   ├─ Board[9] (game state)                      │   │   │
│ │ │   ├─ IsGameActive                               │   │   │
│ │ │   ├─ IsConnected                                │   │   │
│ │ │   └─ Status messages                            │   │   │
│ │ │  Player Data:                                   │   │   │
│ │ │   ├─ PlayerName                                 │   │   │
│ │ │   └─ OpponentName                               │   │   │
│ │ │  Chat Data:                                     │   │   │
│ │ │   └─ ChatMessages (ObservableCollection)        │   │   │
│ │ │                                                 │   │   │
│ │ │ ChatMessage.cs                                  │   │   │
│ │ │   ├─ Id (GUID)                                  │   │   │
│ │ │   ├─ Sender                                     │   │   │
│ │ │   ├─ Message                                    │   │   │
│ │ │   ├─ Timestamp                                  │   │   │
│ │ │   ├─ IsMine (bool)                              │   │   │
│ │ │   ├─ IsSeen (bool)                              │   │   │
│ │ │   └─ Display properties                         │   │   │
│ │ └─────────────────────────────────────────────────┘   │   │
│ └───────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                              MESSAGE FLOW                                    │
└─────────────────────────────────────────────────────────────────────────────┘

GAME START SEQUENCE:
┌────────┐                  ┌────────┐                  ┌────────┐
│Alice   │                  │Server  │                  │  Bob   │
└───┬────┘                  └───┬────┘                  └───┬────┘
    │                           │                           │
    │─────TCP Connect──────────►│                           │
    │◄────PLAYER:X──────────────│                           │
    │─────NAME:Alice───────────►│                           │
    │                           │◄────TCP Connect───────────│
    │                           │─────PLAYER:Y─────────────►│
    │◄────OPPONENT_NAME:Bob─────│                           │
    │                           │◄────NAME:Bob──────────────│
    │                           │─────OPPONENT_NAME:Alice──►│
    │◄────START─────────────────┤─────START────────────────►│
    │◄────BOARD:         ───────┤─────BOARD:         ──────►│
    │◄────TURN:X────────────────┤─────TURN:X───────────────►│


GAME MOVE SEQUENCE:
┌────────┐                  ┌────────┐                  ┌────────┐
│Alice(X)│                  │Server  │                  │Bob(Y)  │
└───┬────┘                  └───┬────┘                  └───┬────┘
    │                           │                           │
    │─────MOVE:4───────────────►│                           │
    │                           │─[Validate]                │
    │                           │─[Update Board]            │
    │◄────BOARD:    X    ───────┤─────BOARD:    X    ──────►│
    │◄────TURN:Y────────────────┤─────TURN:Y───────────────►│
    │                           │                           │
    │                           │◄────MOVE:0────────────────│
    │                           │─[Validate]                │
    │                           │─[Update Board]            │
    │◄────BOARD:Y   X    ───────┤─────BOARD:Y   X    ──────►│
    │◄────TURN:X────────────────┤─────TURN:X───────────────►│


CHAT MESSAGE SEQUENCE:
┌────────┐                  ┌────────┐                  ┌────────┐
│Alice   │                  │Server  │                  │  Bob   │
└───┬────┘                  └───┬────┘                  └───┬────┘
    │                           │                           │
    │─CHAT:abc123:Hello!───────►│                           │
    │                           │─[Route to Bob]            │
    │                           │─CHAT:abc123:Alice:────────►│
    │                           │      Hello!:14:30:45      │
    │                           │                           │
    │                           │◄────SEEN:abc123───────────│
    │◄────SEEN:abc123───────────│                           │
    │  [✓✓ appears]             │                           │


┌─────────────────────────────────────────────────────────────────────────────┐
│                             DATA STRUCTURES                                  │
└─────────────────────────────────────────────────────────────────────────────┘

SERVER STATE:
┌─────────────────────────────────────────┐
│ board = ['X', ' ', 'Y', ' ', 'X', ...]  │
│ currentPlayer = 'Y'                     │
│ gameActive = true                       │
│                                         │
│ playerMap = {                           │
│   'X' → TcpClient (Alice)               │
│   'Y' → TcpClient (Bob)                 │
│ }                                       │
│                                         │
│ playerNames = {                         │
│   'X' → "Alice"                         │
│   'Y' → "Bob"                           │
│ }                                       │
└─────────────────────────────────────────┘

CLIENT STATE (Alice):
┌─────────────────────────────────────────┐
│ MySymbol = 'X'                          │
│ CurrentTurn = 'Y'                       │
│ PlayerName = "Alice"                    │
│ OpponentName = "Bob"                    │
│ IsGameActive = true                     │
│ IsConnected = true                      │
│                                         │
│ Board = ['X', ' ', 'Y', ' ', 'X', ...]  │
│                                         │
│ ChatMessages = [                        │
│   {                                     │
│     Id: "abc123"                        │
│     Sender: "Alice"                     │
│     Message: "Hello!"                   │
│     Timestamp: 14:30:45                 │
│     IsMine: true                        │
│     IsSeen: true   (✓✓)                 │
│   },                                    │
│   {                                     │
│     Id: "def456"                        │
│     Sender: "Bob"                       │
│     Message: "Hi!"                      │
│     Timestamp: 14:30:50                 │
│     IsMine: false                       │
│     IsSeen: false                       │
│   }                                     │
│ ]                                       │
└─────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                           THREADING MODEL                                    │
└─────────────────────────────────────────────────────────────────────────────┘

SERVER:
┌─────────────────────────────────────────┐
│ Main Thread                             │
│  └─ AcceptClients() loop                │
│      ├─ Accept Player X                 │
│      │   └─ Spawn: HandleClient(X)──┐   │
│      │                               │   │
│      └─ Accept Player Y              │   │
│          └─ Spawn: HandleClient(Y)─┐ │   │
└─────────────────────────────────────┼─┼───┘
                                      │ │
┌─────────────────────────────────────▼─┼───┐
│ Background Thread: Player X Handler   │   │
│  └─ while (connected)                 │   │
│      ├─ ReadLineAsync()               │   │
│      ├─ Process message               │   │
│      └─ Broadcast updates             │   │
└───────────────────────────────────────┘   │
                                            │
┌───────────────────────────────────────────▼┐
│ Background Thread: Player Y Handler        │
│  └─ while (connected)                      │
│      ├─ ReadLineAsync()                    │
│      ├─ Process message                    │
│      └─ Broadcast updates                  │
└────────────────────────────────────────────┘

CLIENT:
┌─────────────────────────────────────────┐
│ UI Thread (WPF Dispatcher)              │
│  ├─ Handle button clicks                │
│  ├─ Update game board                   │
│  ├─ Update chat display                 │
│  └─ Dispatcher.Invoke() for cross-thread│
└─────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│ Background Thread: Server Listener      │
│  └─ while (connected)                   │
│      ├─ ReadLineAsync()                 │
│      ├─ Parse message                   │
│      └─ Fire MessageReceived event      │
│          (handled on UI thread)         │
└─────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                         SYNCHRONIZATION                                      │
└─────────────────────────────────────────────────────────────────────────────┘

SERVER:
• lock(lockObj) for game state modifications
• Ensures atomic operations on:
  - board[]
  - currentPlayer
  - gameActive
  - playerMap
  - playerNames

CLIENT:
• Dispatcher.Invoke() for UI updates from background threads
• ObservableCollection auto-updates UI (WPF binding)
• Event-driven architecture prevents race conditions
