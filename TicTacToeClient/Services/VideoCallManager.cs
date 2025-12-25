using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AForge.Video;
using AForge.Video.DirectShow;

namespace TicTacToeClient.Services
{
    /// <summary>
    /// Manages video call functionality including webcam capture and streaming
    /// </summary>
    public class VideoCallManager
    {
        private VideoCaptureDevice? videoSource;
        private TcpListener? streamListener;
        private TcpClient? sendClient;        // For sending our video
        private TcpClient? receiveClient;     // For receiving their video
        private NetworkStream? sendStream;
        private NetworkStream? receiveStream;
        private CancellationTokenSource? cancellationTokenSource;
        private bool isStreaming = false;
        private int videoPort;

        public event Action<Bitmap>? LocalFrameReceived;
        public event Action<Bitmap>? RemoteFrameReceived;
        public event Action<string>? ErrorOccurred;
        public event Action? StreamingStarted;
        public event Action? StreamingStopped;

        public bool IsActive => isStreaming;
        public int StreamPort => videoPort;

        /// <summary>
        /// Starts the local webcam capture
        /// </summary>
        public bool StartWebcam()
        {
            try
            {
                // Get available video devices
                FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                
                if (videoDevices.Count == 0)
                {
                    ErrorOccurred?.Invoke("No webcam found");
                    return false;
                }

                // Use the first available webcam
                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                
                // Set desired resolution (640x480 is common and supported by most webcams)
                if (videoSource.VideoCapabilities.Length > 0)
                {
                    var capability = videoSource.VideoCapabilities[0];
                    foreach (var cap in videoSource.VideoCapabilities)
                    {
                        if (cap.FrameSize.Width == 640 && cap.FrameSize.Height == 480)
                        {
                            capability = cap;
                            break;
                        }
                    }
                    videoSource.VideoResolution = capability;
                }

                // Subscribe to new frame event
                videoSource.NewFrame += VideoSource_NewFrame;
                videoSource.Start();

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to start webcam: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stops the local webcam capture
        /// </summary>
        public void StopWebcam()
        {
            try
            {
                if (videoSource != null && videoSource.IsRunning)
                {
                    videoSource.SignalToStop();
                    videoSource.WaitForStop();
                    videoSource.NewFrame -= VideoSource_NewFrame;
                    videoSource = null;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Error stopping webcam: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles new frames from the webcam
        /// </summary>
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                // Clone the frame to avoid disposal issues
                Bitmap frame = (Bitmap)eventArgs.Frame.Clone();
                
                // Notify UI of new local frame
                LocalFrameReceived?.Invoke((Bitmap)frame.Clone());

                // If streaming is active, send frame to remote peer
                if (isStreaming && sendStream != null && sendStream.CanWrite && sendClient != null && sendClient.Connected)
                {
                    SendFrame(frame);
                }
                else
                {
                    frame.Dispose();
                    // Only log occasionally to avoid spam
                    if (DateTime.Now.Second % 5 == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[VIDEO] Not sending frame - isStreaming:{isStreaming}, sendStream null:{sendStream == null}, sendClient connected:{sendClient?.Connected}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VIDEO] Error processing frame: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a video frame over the network
        /// </summary>
        private void SendFrame(Bitmap frame)
        {
            try
            {
                // Check connection before sending
                if (sendStream == null || !sendStream.CanWrite || sendClient == null || !sendClient.Connected)
                {
                    frame.Dispose();
                    return;
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    // Compress frame as JPEG
                    ImageCodecInfo jpegCodec = GetEncoder(ImageFormat.Jpeg);
                    if (jpegCodec == null)
                    {
                        frame.Dispose();
                        return;
                    }
                    
                    EncoderParameters encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 50L); // Quality 50%
                    
                    frame.Save(ms, jpegCodec, encoderParams);
                    byte[] frameData = ms.ToArray();

                    // Send frame size first (4 bytes)
                    byte[] sizeData = BitConverter.GetBytes(frameData.Length);
                    
                    lock (sendStream)
                    {
                        sendStream.Write(sizeData, 0, 4);
                        // Send frame data
                        sendStream.Write(frameData, 0, frameData.Length);
                        sendStream.Flush();
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[VIDEO] Sent frame: {frameData.Length} bytes");
                }
            }
            catch (System.IO.IOException)
            {
                // Connection closed, stop streaming
                isStreaming = false;
                System.Diagnostics.Debug.WriteLine("[VIDEO] Send failed - connection closed");
            }
            catch (ObjectDisposedException)
            {
                // Stream disposed, stop streaming
                isStreaming = false;
            }
            catch (Exception ex)
            {
                // Suppress errors to avoid spamming, streaming might be interrupted
                System.Diagnostics.Debug.WriteLine($"[VIDEO] Send frame error: {ex.Message}");
            }
            finally
            {
                frame.Dispose();
            }
        }

        /// <summary>
        /// Gets the JPEG encoder
        /// </summary>
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null!;
        }

        /// <summary>
        /// Starts listening for incoming video stream (as receiver)
        /// Only accepts ONE connection for receiving their video
        /// </summary>
        public async Task<bool> StartListeningAsync()
        {
            try
            {
                // Find an available port
                videoPort = GetAvailablePort();
                
                streamListener = new TcpListener(IPAddress.Any, videoPort);
                streamListener.Start();

                cancellationTokenSource = new CancellationTokenSource();
                
                System.Diagnostics.Debug.WriteLine($"[VIDEO] Listening on port {videoPort} for incoming video...");
                
                // Wait for ONE connection to receive their video
                _ = Task.Run(async () =>
                {
                    try
                    {
                        receiveClient = await streamListener.AcceptTcpClientAsync();
                        receiveStream = receiveClient.GetStream();
                        System.Diagnostics.Debug.WriteLine("[VIDEO] Incoming video connection accepted!");
                        
                        // Start receiving frames
                        await ReceiveFramesAsync(cancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[VIDEO] Error accepting connection: {ex.Message}");
                        ErrorOccurred?.Invoke($"Error accepting video connection: {ex.Message}");
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to start listening: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Connects to remote peer's video stream to send our video
        /// </summary>
        public async Task<bool> ConnectToStreamAsync(string remoteIP, int remotePort)
        {
            const int maxRetries = 3;
            const int retryDelayMs = 500;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[VIDEO] Connecting to {remoteIP}:{remotePort} to send video (attempt {attempt}/{maxRetries})...");
                    
                    // Connect to send our video to their listening port
                    sendClient = new TcpClient();
                    
                    // Set timeouts
                    sendClient.SendTimeout = 5000;
                    sendClient.ReceiveTimeout = 5000;
                    
                    // Use a connection timeout
                    var connectTask = sendClient.ConnectAsync(remoteIP, remotePort);
                    if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                    {
                        throw new TimeoutException("Connection timed out");
                    }
                    
                    await connectTask; // Ensure any exception is thrown
                    
                    sendStream = sendClient.GetStream();
                    System.Diagnostics.Debug.WriteLine("[VIDEO] Outgoing video connection established!");
                    
                    isStreaming = true;
                    StreamingStarted?.Invoke();

                    if (cancellationTokenSource == null)
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[VIDEO] Connection attempt {attempt} failed: {ex.Message}");
                    
                    // Clean up failed connection
                    try { sendClient?.Close(); } catch { }
                    sendClient = null;
                    sendStream = null;
                    
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelayMs);
                    }
                    else
                    {
                        ErrorOccurred?.Invoke($"Failed to connect to video stream after {maxRetries} attempts: {ex.Message}");
                        return false;
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Receives video frames from the network
        /// </summary>
        private async Task ReceiveFramesAsync(CancellationToken cancellationToken)
        {
            byte[] sizeBuffer = new byte[4];
            
            try
            {
                while (!cancellationToken.IsCancellationRequested && receiveStream != null && receiveStream.CanRead)
                {
                    // Check if data is available to avoid blocking
                    if (!receiveClient!.Connected)
                    {
                        break; // Connection lost
                    }

                    // Read frame size with timeout check
                    int bytesRead = 0;
                    while (bytesRead < 4 && !cancellationToken.IsCancellationRequested)
                    {
                        if (!receiveStream.CanRead || !receiveClient.Connected)
                        {
                            return; // Connection closed
                        }
                        
                        int read = await receiveStream.ReadAsync(sizeBuffer, bytesRead, 4 - bytesRead, cancellationToken);
                        if (read == 0) return; // Connection closed
                        bytesRead += read;
                    }

                    int frameSize = BitConverter.ToInt32(sizeBuffer, 0);
                    
                    if (frameSize <= 0 || frameSize > 5000000) // Max 5MB per frame
                    {
                        continue;
                    }

                    // Read frame data
                    byte[] frameData = new byte[frameSize];
                    bytesRead = 0;
                    while (bytesRead < frameSize && !cancellationToken.IsCancellationRequested)
                    {
                        if (!receiveStream.CanRead || !receiveClient.Connected)
                        {
                            return; // Connection closed
                        }
                        
                        int read = await receiveStream.ReadAsync(frameData, bytesRead, frameSize - bytesRead, cancellationToken);
                        if (read == 0) return; // Connection closed
                        bytesRead += read;
                    }

                    // Decode frame
                    using (MemoryStream ms = new MemoryStream(frameData))
                    {
                        Bitmap frame = new Bitmap(ms);
                        System.Diagnostics.Debug.WriteLine($"[VIDEO] Received frame: {frameSize} bytes");
                        RemoteFrameReceived?.Invoke(frame);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, don't report error
            }
            catch (System.IO.IOException)
            {
                // Connection closed by remote, don't report error
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine($"[VIDEO] Error receiving frames: {ex.Message}");
                }
            }
            finally
            {
                StreamingStopped?.Invoke();
            }
        }

        /// <summary>
        /// Stops the video stream
        /// </summary>
        public void StopStreaming()
        {
            try
            {
                isStreaming = false;
                cancellationTokenSource?.Cancel();

                // Give a moment for cancellation to propagate
                System.Threading.Thread.Sleep(100);

                try
                {
                    sendStream?.Close();
                }
                catch { }

                try
                {
                    receiveStream?.Close();
                }
                catch { }

                try
                {
                    sendClient?.Close();
                }
                catch { }

                try
                {
                    receiveClient?.Close();
                }
                catch { }

                try
                {
                    streamListener?.Stop();
                }
                catch { }

                sendStream = null;
                receiveStream = null;
                sendClient = null;
                receiveClient = null;
                streamListener = null;
                cancellationTokenSource = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VIDEO] Error stopping stream: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets an available TCP port that works for LAN connections
        /// </summary>
        private int GetAvailablePort()
        {
            // Use IPAddress.Any to ensure the port works for all network interfaces
            TcpListener listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            System.Diagnostics.Debug.WriteLine($"[VIDEO] Found available port: {port}");
            return port;
        }

        /// <summary>
        /// Cleans up resources
        /// </summary>
        public void Dispose()
        {
            StopWebcam();
            StopStreaming();
        }
    }
}
