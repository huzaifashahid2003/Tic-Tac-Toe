# Quick Start Guide - LAN Setup

## 🚀 Connect Two Laptops for Multiplayer

### Prerequisites
- Two laptops (Laptop A and Laptop B)
- Both connected to **same Wi-Fi network**
- .NET 8.0 SDK installed on both

---

## Step-by-Step Setup

### 🖥️ On Laptop A (Server + Player 1)

#### 1. Find Your IP Address
Open PowerShell/Command Prompt:
```powershell
ipconfig
```

Look for **IPv4 Address** under your Wi-Fi adapter:
```
Wireless LAN adapter Wi-Fi:
   IPv4 Address. . . . . . . . : 192.168.1.100
```
📝 **Write down this IP!** (e.g., `192.168.1.100`)

#### 2. Start the Server
```bash
cd C:\Users\Huzaifa\OneDrive\Desktop\NP_Project\TicTacToeServer
dotnet run
```

You should see:
```
===========================================
  SERVER STARTED ON PORT 5000
===========================================
Local IP Address: 192.168.1.100
Clients should connect to: 192.168.1.100:5000
===========================================
```

#### 3. Start Client on Laptop A
Open **another terminal** (keep server running):
```bash
cd C:\Users\Huzaifa\OneDrive\Desktop\NP_Project\TicTacToeClient
dotnet run
```

In the client window:
- **Your Name:** `Alice`
- **Server IP:** `192.168.1.100` (your laptop's IP)
- **Port:** `5000`
- Click **Connect**

Status should show: "Connected! Waiting for opponent..."

---

### 💻 On Laptop B (Player 2)

#### 1. Copy the Project (or use shared folder)
Option A: Copy entire `NP_Project` folder to Laptop B

Option B: Use network share/USB/Git

#### 2. Start Client on Laptop B
```bash
cd C:\Path\To\NP_Project\TicTacToeClient
dotnet run
```

In the client window:
- **Your Name:** `Bob`
- **Server IP:** `192.168.1.100` (Laptop A's IP that you noted)
- **Port:** `5000`
- Click **Connect**

---

## ✅ Success!

Both clients should now show:
- "Game started! You are Player X/Y"
- Player X's turn indicator
- Chat enabled

### Test the Chat:
1. Alice types "Hello!" and presses Enter
2. Bob sees the message with timestamp
3. Alice sees "✓✓" (seen status) after Bob views it

### Play the Game:
1. Player X clicks a cell
2. Board updates on both screens
3. Turn switches to Player Y

---

## 🔥 Firewall Warning

If connection fails, Windows Firewall might be blocking port 5000.

### Allow Port 5000:
1. Open **Windows Defender Firewall**
2. Click **Advanced Settings**
3. Click **Inbound Rules** → **New Rule**
4. Select **Port** → Next
5. Select **TCP**, enter **5000** → Next
6. Select **Allow the connection** → Next
7. Check all profiles → Next
8. Name it "Tic Tac Toe Server" → Finish

---

## 🛠️ Troubleshooting

### Problem: "Connection failed"
**Solution:**
- Verify both laptops on same Wi-Fi
- Check server is running on Laptop A
- Ping Laptop A from Laptop B:
  ```bash
  ping 192.168.1.100
  ```
- Check firewall settings

### Problem: "Server IP not found"
**Solution:**
- Make sure you're using Laptop A's **local IP** (starts with 192.168.x.x or 10.x.x.x)
- Don't use `127.0.0.1` for remote connections
- Re-check `ipconfig` output

### Problem: Server shows error on startup
**Solution:**
- Port 5000 might be in use
- Close any running instances
- Or change port in both server and client code

### Problem: Build error "file locked"
**Solution:**
- Close all running server/client windows
- Run: `dotnet clean`
- Then: `dotnet build`

---

## 📱 Alternative: Mobile Hotspot

If no Wi-Fi available:

1. **Laptop A:** Enable mobile hotspot (Settings → Network → Mobile Hotspot)
2. **Laptop B:** Connect to Laptop A's hotspot
3. Follow same steps as above
4. Use Laptop A's hotspot IP address

---

## 🎮 Quick Test on Single Laptop (Localhost)

For testing without second laptop:

**Terminal 1:** Server
```bash
cd TicTacToeServer
dotnet run
```

**Terminal 2:** Client 1
```bash
cd TicTacToeClient
dotnet run
# Use IP: 127.0.0.1 or localhost
```

**Terminal 3:** Client 2
```bash
cd TicTacToeClient
dotnet run
# Use IP: 127.0.0.1 or localhost
```

⚠️ Note: For your academic project, you need to demonstrate **actual laptop-to-laptop** connection!

---

## 📊 Network Diagram

```
┌─────────────────────────────────────┐
│         Wi-Fi Router                │
│      (192.168.1.1)                  │
└──────────┬─────────────┬────────────┘
           │             │
   ┌───────▼──────┐  ┌──▼────────────┐
   │  Laptop A    │  │  Laptop B     │
   │  192.168.1.100│ │  192.168.1.101│
   │              │  │               │
   │  Server      │  │  Client       │
   │  Client      │  │               │
   └──────────────┘  └───────────────┘
```

---

## 🎯 Expected Console Output

### Server Console:
```
[CONNECTION] Client connecting... Current player count: 1
[ASSIGNMENT] Assigning player symbol: X
[CONNECTED] Player X connected successfully (Total players: 1/2)
[NAME] Player X set name to: Alice

[CONNECTION] Client connecting... Current player count: 2
[ASSIGNMENT] Assigning player symbol: Y
[CONNECTED] Player Y connected successfully (Total players: 2/2)
[NAME] Player Y set name to: Bob
[GAME START] Both players connected. Game starting!

[TURN] Starting with Player X
[PROCESSING] Processing move from Player X at cell 4
[MOVE] Player X moved to cell 4
[TURN] Switched to Player Y

[CHAT] Alice: Hello!
[CHAT] Bob: Hi Alice!
[SEEN] Player X marked message abc123 as seen
```

### Client Console (Alice):
```
Connected! Waiting for opponent...
Playing against Bob
Game started! You are Player X
🎮 Your turn!
```

---

**Now you're ready to play! 🎮💬**
