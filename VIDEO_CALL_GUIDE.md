# Video Call Feature - User Guide

## Overview

The video call feature allows two players to have a live video conversation while playing Tic Tac Toe. The feature uses:
- **Webcam capture** via AForge.NET
- **P2P video streaming** over TCP
- **Server-based signaling** for call control

---

## Prerequisites

### Hardware Requirements
- **Webcam/Camera** on both laptops
- Both laptops on the **same Wi-Fi/LAN network**

### Software Requirements
- **.NET 8.0** installed
- **Windows OS** (for webcam access via DirectShow)
- **Firewall configured** to allow incoming TCP connections

---

## How to Use Video Call

### Step 1: Connect to Game
1. Start the server on one machine
2. Connect both clients to the server
3. Wait for both players to join

### Step 2: Initiate Video Call

**Player A (Caller):**
1. Click the **📹 Video Call** button
2. Wait for Player B to respond

**Player B (Receiver):**
1. A popup appears: **"[Player Name] is calling you"**
2. Click **"Yes"** to accept or **"No"** to reject

### Step 3: If Call is Accepted

**Player B (Receiver):**
1. Video window opens automatically
2. Webcam starts capturing video
3. A message shows: **"Waiting for connection on port [XXXX]"**
4. **Share this port number** with Player A (via chat or verbally)

**Player A (Caller):**
1. A dialog box appears: **"Enter opponent's video port"**
2. **Enter the port number** that Player B shared
3. Click OK
4. Video window opens and connects

### Step 4: During the Call

**Video Window Layout:**
- **Left panel:** Your camera feed
- **Right panel:** Opponent's camera feed
- **Bottom:** Call status and duration timer
- **End Call button:** Red button to terminate call

**Features:**
- Real-time video streaming (JPEG compressed)
- Call duration timer (MM:SS)
- Simultaneous two-way video

### Step 5: Ending the Call

**Either player can:**
1. Click the **"End Call"** button, OR
2. Close the video window

**Result:**
- Both video windows close
- Webcams stop
- Players return to the game

---

## Troubleshooting

### Problem: "No webcam found"
**Solution:**
- Ensure webcam is connected and enabled
- Check Device Manager (Windows)
- Grant camera permissions to the application

### Problem: "Failed to connect to video stream"
**Solution:**
- Verify both players are on same network
- Check firewall settings
- Ensure correct port number was entered
- Try using `ipconfig` to verify IP addresses

### Problem: Video is laggy or delayed
**Causes:**
- Slow Wi-Fi connection
- High network traffic
- Low-end webcam

**Solutions:**
- Use 5GHz Wi-Fi instead of 2.4GHz
- Close other network-heavy applications
- Reduce webcam resolution (default: 640x480)

### Problem: Firewall blocks connection
**Solution (Windows):**
1. Open **Windows Defender Firewall**
2. Click **"Allow an app through firewall"**
3. Add `TicTacToeClient.exe`
4. Enable for **Private networks**

---

## Technical Details

### Video Streaming
- **Codec:** JPEG compression (50% quality)
- **Resolution:** 640x480 (default)
- **Frame Rate:** ~15-30 FPS (depends on network)
- **Transport:** TCP (reliable delivery)

### Port Allocation
- **Game server:** Fixed port 5000
- **Video streaming:** Random available port (dynamic)
- Ports are automatically selected by the OS

### Architecture
```
Player A                    Server                     Player B
   │                          │                           │
   ├─CALL_REQUEST────────────►│                           │
   │                          ├─CALL_REQUEST:Name────────►│
   │                          │                           │
   │                          │◄──────CALL_ACCEPT─────────┤
   │◄─────CALL_ACCEPT─────────┤                           │
   │                          │                           │
   │                          │                      [Starts listening]
   │                          │                      Port: 54321
   │◄─────────────[Port 54321 shared manually]───────────►│
   │                                                       │
   ├──────────────[Direct P2P Video Stream]──────────────►│
   │◄──────────────[Direct P2P Video Stream]──────────────┤
   │                                                       │
```

### Security Considerations
- Video is **not encrypted** (plain TCP)
- Suitable for **trusted LAN environments** only
- Not recommended for public/untrusted networks
- No authentication beyond game connection

---

## Limitations

1. **Manual port sharing:** Players must communicate port numbers (not automated)
2. **LAN only:** Does not work over internet without port forwarding
3. **No audio:** Video only, no microphone support
4. **Windows only:** DirectShow webcam access is Windows-specific
5. **Two players only:** No group video calls

---

## Future Improvements

Potential enhancements (not currently implemented):
- Automatic port exchange through server
- Audio streaming support
- End-to-end encryption
- Cross-platform webcam support (Linux/Mac)
- Video quality settings
- Snapshot/screenshot feature

---

## Support

For issues or questions:
1. Check firewall and network settings
2. Verify webcam functionality in other apps
3. Ensure both clients are on same LAN
4. Review server console logs for errors

**Note:** This is an educational project demonstrating video streaming concepts. For production use, consider mature solutions like WebRTC, Zoom SDK, or Agora.io.
