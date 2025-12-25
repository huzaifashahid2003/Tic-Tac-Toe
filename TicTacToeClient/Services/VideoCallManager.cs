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
        private TcpClient? streamClient;
        private NetworkStream? streamNetworkStream;
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
                LocalFrameReceived?.Invoke(frame);

                // If streaming is active, send frame to remote peer
                if (isStreaming && streamNetworkStream != null && streamNetworkStream.CanWrite)
                {
                    SendFrame(frame);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Error processing frame: {ex.Message}");
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
                if (streamNetworkStream == null || !streamNetworkStream.CanWrite || streamClient == null || !streamClient.Connected)
                {
                    return;
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    // Compress frame as JPEG
                    ImageCodecInfo jpegCodec = GetEncoder(ImageFormat.Jpeg);
                    EncoderParameters encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 50L); // Quality 50%
                    
                    frame.Save(ms, jpegCodec, encoderParams);
                    byte[] frameData = ms.ToArray();

                    // Send frame size first (4 bytes)
                    byte[] sizeData = BitConverter.GetBytes(frameData.Length);
                    streamNetworkStream.Write(sizeData, 0, 4);

                    // Send frame data
                    streamNetworkStream.Write(frameData, 0, frameData.Length);
                    streamNetworkStream.Flush();
                }
            }
            catch (System.IO.IOException)
            {
                // Connection closed, stop streaming
                isStreaming = false;
            }
            catch (Exception ex)
            {
                // Suppress errors to avoid spamming, streaming might be interrupted
                System.Diagnostics.Debug.WriteLine($"[VIDEO] Send frame error: {ex.Message}");
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
                
                // Wait for connection in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        streamClient = await streamListener.AcceptTcpClientAsync();
                        streamNetworkStream = streamClient.GetStream();
                        isStreaming = true;
                        
                        StreamingStarted?.Invoke();
                        
                        // Start receiving frames
                        await ReceiveFramesAsync(cancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
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
        /// Connects to remote peer's video stream (as sender)
        /// </summary>
        public async Task<bool> ConnectToStreamAsync(string remoteIP, int remotePort)
        {
            try
            {
                streamClient = new TcpClient();
                await streamClient.ConnectAsync(remoteIP, remotePort);
                streamNetworkStream = streamClient.GetStream();
                isStreaming = true;

                cancellationTokenSource = new CancellationTokenSource();
                
                StreamingStarted?.Invoke();

                // Start receiving frames in background
                _ = Task.Run(async () => await ReceiveFramesAsync(cancellationTokenSource.Token));

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to connect to video stream: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Receives video frames from the network
        /// </summary>
        private async Task ReceiveFramesAsync(CancellationToken cancellationToken)
        {
            byte[] sizeBuffer = new byte[4];
            
            try
            {
                while (!cancellationToken.IsCancellationRequested && streamNetworkStream != null && streamNetworkStream.CanRead)
                {
                    // Check if data is available to avoid blocking
                    if (!streamClient!.Connected)
                    {
                        break; // Connection lost
                    }

                    // Read frame size with timeout check
                    int bytesRead = 0;
                    while (bytesRead < 4 && !cancellationToken.IsCancellationRequested)
                    {
                        if (!streamNetworkStream.CanRead || !streamClient.Connected)
                        {
                            return; // Connection closed
                        }
                        
                        int read = await streamNetworkStream.ReadAsync(sizeBuffer, bytesRead, 4 - bytesRead, cancellationToken);
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
                        if (!streamNetworkStream.CanRead || !streamClient.Connected)
                        {
                            return; // Connection closed
                        }
                        
                        int read = await streamNetworkStream.ReadAsync(frameData, bytesRead, frameSize - bytesRead, cancellationToken);
                        if (read == 0) return; // Connection closed
                        bytesRead += read;
                    }

                    // Decode frame
                    using (MemoryStream ms = new MemoryStream(frameData))
                    {
                        Bitmap frame = new Bitmap(ms);
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
                    streamNetworkStream?.Close();
                }
                catch { }

                try
                {
                    streamClient?.Close();
                }
                catch { }

                try
                {
                    streamListener?.Stop();
                }
                catch { }

                streamNetworkStream = null;
                streamClient = null;
                streamListener = null;
                cancellationTokenSource = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VIDEO] Error stopping stream: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets an available TCP port
        /// </summary>
        private int GetAvailablePort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
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
