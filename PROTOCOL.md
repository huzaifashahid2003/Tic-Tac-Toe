# TCP Protocol Specification

## Network Programming Protocol Documentation

This document describes the custom TCP protocol used in the Tic Tac Toe multiplayer game.

---

## Protocol Overview

- **Transport:** TCP (Transmission Control Protocol)
- **Port:** 5000 (configurable)
- **Format:** Text-based, line-delimited
- **Encoding:** UTF-8
- **Message Delimiter:** `\n` (newline)

---

## Connection Flow

```
┌────────┐                      ┌────────┐
│ Client │                      │ Server │
└───┬────┘                      └───┬────┘
    │                               │
    │    TCP Connect (3-way HS)     │
    ├──────────────────────────────►│
    │                               │
    │      PLAYER:X or PLAYER:Y     │
    │◄──────────────────────────────┤
    │                               │
    │       NAME:PlayerName         │
    ├──────────────────────────────►│
    │                               │
    │   OPPONENT_NAME:OtherName     │
    │◄──────────────────────────────┤
    │                               │
    │           START               │
    │◄──────────────────────────────┤
    │                               │
    │       BOARD:         (9 chars)│
    │◄──────────────────────────────┤
    │                               │
    │          TURN:X               │
    │◄──────────────────────────────┤
    │                               │
```

---

## Message Types

### 1. Connection Messages

#### `PLAYER:{symbol}`
**Direction:** Server → Client  
**Purpose:** Assign player symbol  
**Format:** `PLAYER:X` or `PLAYER:Y`  
**Example:**
```
PLAYER:X
```
**When:** Immediately after client connects

---

#### `NAME:{playerName}`
**Direction:** Client → Server  
**Purpose:** Set player display name  
**Format:** `NAME:{string}`  
**Example:**
```
NAME:Alice
```
**Constraints:**
- Sent by client after receiving PLAYER assignment
- Name should not contain colons (`:`)

---

#### `OPPONENT_NAME:{name}`
**Direction:** Server → Client  
**Purpose:** Notify about opponent's name  
**Format:** `OPPONENT_NAME:{string}`  
**Example:**
```
OPPONENT_NAME:Bob
```
**When:** After opponent sends NAME message

---

### 2. Game State Messages

#### `START`
**Direction:** Server → Both Clients  
**Purpose:** Signal game start  
**Format:** `START`  
**When:** Both players connected

---

#### `BOARD:{state}`
**Direction:** Server → Both Clients  
**Purpose:** Send current board state  
**Format:** `BOARD:{9 characters}`  
**Characters:**
- `' '` (space) = empty cell
- `'X'` = Player X's mark
- `'Y'` = Player Y's mark

**Example:**
```
BOARD:X  Y X Y 
```
Represents:
```
X |   |  
---------
Y | X | Y
---------
  |   |  
```

**Cell Indices:**
```
0 | 1 | 2
---------
3 | 4 | 5
---------
6 | 7 | 8
```

---

#### `TURN:{player}`
**Direction:** Server → Both Clients  
**Purpose:** Indicate whose turn it is  
**Format:** `TURN:X` or `TURN:Y`  
**Example:**
```
TURN:X
```

---

#### `MOVE:{cellIndex}`
**Direction:** Client → Server  
**Purpose:** Make a move  
**Format:** `MOVE:{0-8}`  
**Example:**
```
MOVE:4
```
**Validation:**
- Cell must be empty (` `)
- Must be player's turn
- Index must be 0-8

---

#### `WIN:{winner}`
**Direction:** Server → Both Clients  
**Purpose:** Announce winner  
**Format:** `WIN:X` or `WIN:Y`  
**Example:**
```
WIN:X
```
**When:** Player gets 3 in a row

---

#### `DRAW`
**Direction:** Server → Both Clients  
**Purpose:** Announce draw  
**Format:** `DRAW`  
**When:** Board is full with no winner

---

#### `RESTART`
**Direction:** Client → Server  
**Purpose:** Request new game  
**Format:** `RESTART`  
**Server Response:** Sends START, BOARD, TURN to both clients

---

### 3. Chat Messages

#### `CHAT:{messageId}:{text}` (Client → Server)
**Direction:** Client → Server  
**Purpose:** Send chat message  
**Format:** `CHAT:{guid}:{message}`  
**Example:**
```
CHAT:a1b2c3d4:Hello there!
```
**Fields:**
- `messageId`: Unique identifier (GUID)
- `text`: Message content (max 500 chars)

---

#### `CHAT:{messageId}:{sender}:{text}:{timestamp}` (Server → Client)
**Direction:** Server → Client  
**Purpose:** Deliver chat message  
**Format:** `CHAT:{guid}:{sender}:{message}:{ISO8601}`  
**Example:**
```
CHAT:a1b2c3d4:Alice:Hello there!:2025-12-25 14:30:45
```
**Fields:**
- `messageId`: Original message ID
- `sender`: Sender's name
- `text`: Message content
- `timestamp`: Server timestamp (yyyy-MM-dd HH:mm:ss)

---

### 4. Video Call Messages

#### `CALL_REQUEST` (Client → Server)
**Direction:** Client → Server → Opponent  
**Purpose:** Initiate video call request  
**Format:** `CALL_REQUEST`  
**Example:**
```
CALL_REQUEST
```
**Server Action:** Routes to opponent as `CALL_REQUEST:{callerName}`

---

#### `CALL_REQUEST:{callerName}` (Server → Client)
**Direction:** Server → Opponent  
**Purpose:** Notify about incoming call  
**Format:** `CALL_REQUEST:{string}`  
**Example:**
```
CALL_REQUEST:Alice
```
**Client Action:** Show popup with Accept/Reject buttons

---

#### `CALL_ACCEPT` (Client → Server → Opponent)
**Direction:** Bidirectional through Server  
**Purpose:** Accept incoming video call  
**Format:** `CALL_ACCEPT`  
**Example:**
```
CALL_ACCEPT
```
**Flow:**
1. Receiver sends `CALL_ACCEPT` to server
2. Server routes to caller
3. Both clients establish P2P video stream

---

#### `CALL_REJECT` (Client → Server → Opponent)
**Direction:** Bidirectional through Server  
**Purpose:** Reject incoming video call  
**Format:** `CALL_REJECT`  
**Example:**
```
CALL_REJECT
```
**Client Action:** Show notification "Call was rejected"

---

#### `CALL_END` (Client → Server → Opponent)
**Direction:** Bidirectional through Server  
**Purpose:** Terminate active video call  
**Format:** `CALL_END`  
**Example:**
```
CALL_END
```
**Client Action:** Close video windows, stop webcam

**Note:** Video stream data is NOT routed through the game server. After signaling (ACCEPT), clients establish direct P2P TCP connection for video frames. Server only handles call control signaling.

---

#### `SEEN:{messageId}`
**Direction:** Bidirectional  
**Purpose:** Mark message as seen  

**Client → Server:**
```
SEEN:a1b2c3d4
```
Client indicates they've viewed a message

**Server → Client:**
```
SEEN:a1b2c3d4
```
Server notifies sender that message was seen

---

### 4. Error Messages

#### `ERROR:{description}`
**Direction:** Server → Client  
**Purpose:** Error notification  
**Format:** `ERROR:{string}`  
**Examples:**
```
ERROR:Not your turn
ERROR:Invalid move
ERROR:Player disconnected
```

---

## State Machine

### Client State Diagram
```
┌─────────────┐
│ Disconnected│
└──────┬──────┘
       │ Connect()
       ▼
┌─────────────┐
│  Connected  │──► Receive PLAYER:X/Y
└──────┬──────┘
       │ Send NAME
       ▼
┌─────────────┐
│   Waiting   │──► Receive OPPONENT_NAME
└──────┬──────┘    Receive START
       │
       ▼
┌─────────────┐
│   Playing   │◄─► Send MOVE, Receive BOARD/TURN
└──────┬──────┘    Send/Receive CHAT
       │
       ├──► Receive WIN/DRAW
       │
       ▼
┌─────────────┐
│  Game Over  │──► Send RESTART
└──────┬──────┘
       │
       └──► Back to Playing
```

### Server State Diagram (per game session)
```
┌─────────────┐
│   Waiting   │
│  (0 players)│
└──────┬──────┘
       │ Player X connects
       ▼
┌─────────────┐
│   Waiting   │
│  (1 player) │
└──────┬──────┘
       │ Player Y connects
       ▼
┌─────────────┐
│   Playing   │──► Validate moves
│   (2 players)│   Route chat
└──────┬──────┘   Broadcast updates
       │
       ├──► Win detected
       ├──► Draw detected
       │
       ▼
┌─────────────┐
│  Game Over  │──► Receive RESTART
└──────┬──────┘
       │
       └──► Back to Playing
```

---

## Win Conditions

The server checks these patterns:

### Rows
```
0,1,2  →  XXX | | 
3,4,5  →   | |XXX
6,7,8  →   | | |XXX
```

### Columns
```
0,3,6  →  X| | 
          X| | 
          X| | 

1,4,7  →   |X| 
           |X| 
           |X| 

2,5,8  →   | |X
           | |X
           | |X
```

### Diagonals
```
0,4,8  →  X| | 
           |X| 
           | |X

2,4,6  →   | |X
           |X| 
          X| | 
```

---

## Timing & Sequence

### Typical Game Session

```
Time  | Client A         | Server           | Client B
------|------------------|------------------|------------------
0ms   | Connect          | Accept           | -
10ms  | ← PLAYER:X       | Send X           | -
20ms  | Send NAME:Alice→ | Store name       | -
100ms | -                | Accept           | Connect
110ms | ← OPPONENT:Bob   | Send Y           | ← PLAYER:Y
120ms | -                | Store name       | Send NAME:Bob →
130ms | ← START          | Both ready       | ← START
135ms | ← BOARD:         | Initial board    | ← BOARD:
140ms | ← TURN:X         | X starts         | ← TURN:X
150ms | Send MOVE:4 →    | Validate         | -
155ms | ← BOARD:    X    | Update           | ← BOARD:    X
160ms | ← TURN:Y         | Switch turn      | ← TURN:Y
200ms | -                | Validate         | Send MOVE:0 →
205ms | ← BOARD:Y   X    | Update           | ← BOARD:Y   X
...   | ...              | ...              | ...
500ms | Send CHAT:... →  | Route            | -
505ms | -                | Deliver          | ← CHAT:Alice:Hi
510ms | -                | Notify seen      | Send SEEN:... →
515ms | ← SEEN:...       | -                | -
```

---

## Error Handling

### Connection Errors
- **Timeout:** 30 seconds without activity
- **Disconnect:** Remove player, notify opponent, end game

### Validation Errors
- **Invalid move:** Send `ERROR:` message, don't update state
- **Out of turn:** Send `ERROR:Not your turn`
- **Occupied cell:** Send `ERROR:Invalid move`

### Chat Errors
- **Message too long:** Truncate to 500 chars
- **Invalid format:** Log error, don't crash

---

## Security Considerations

⚠️ **This is an educational protocol - not production-ready**

Missing features for production:
- No authentication
- No encryption (plaintext)
- No rate limiting
- No input sanitization
- No replay protection
- No checksum/CRC

For production, add:
- TLS/SSL encryption
- Token-based auth
- Input validation
- Message signing
- DDoS protection

---

## Testing the Protocol

### Manual Testing with Telnet

```bash
# Connect to server
telnet 192.168.1.100 5000

# Server responds:
PLAYER:X

# Send name:
NAME:TestPlayer

# Wait for opponent...

# Game starts:
START
BOARD:         
TURN:X

# Make move:
MOVE:4

# Send chat:
CHAT:test123:Hello
```

### Protocol Compliance Checklist

- [ ] Messages are UTF-8 encoded
- [ ] Messages end with `\n`
- [ ] Server assigns X before Y
- [ ] BOARD always has 9 characters
- [ ] Cell indices are 0-8
- [ ] Chat timestamps use yyyy-MM-dd HH:mm:ss format
- [ ] Message IDs are unique (GUIDs)
- [ ] Server validates all moves
- [ ] Turn alternates between X and Y
- [ ] Win/Draw detection is accurate

---

## Performance Metrics

**Expected Latency:**
- Local network: 1-10ms
- Message size: 20-100 bytes
- Bandwidth: < 1 KB/s per client

**Scalability:**
- Current design: 2 clients per server
- For more players: Implement room system

---

## Future Extensions

Possible protocol extensions:
1. `ROOM:{roomId}` - Multiple game rooms
2. `SPECTATE` - Observer mode
3. `UNDO` - Take back move
4. `REMATCH` - Quick rematch
5. `EMOJI:{code}` - Chat reactions
6. `TYPING` - Typing indicator
7. `PING/PONG` - Keep-alive

---

**Protocol Version:** 1.0  
**Last Updated:** December 25, 2025
