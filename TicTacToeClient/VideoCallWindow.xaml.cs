using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TicTacToeClient.Services;

namespace TicTacToeClient
{
    public partial class VideoCallWindow : Window
    {
        private VideoCallManager videoManager;
        private NetworkManager networkManager;
        private DispatcherTimer callTimer;
        private DateTime callStartTime;
        private bool isCallActive = false;
        private bool webcamStarted = false;

        public VideoCallWindow(VideoCallManager videoManager, NetworkManager networkManager, bool delayWebcamStart = false)
        {
            InitializeComponent();
            
            this.videoManager = videoManager;
            this.networkManager = networkManager;

            // Subscribe to video manager events
            videoManager.LocalFrameReceived += OnLocalFrameReceived;
            videoManager.RemoteFrameReceived += OnRemoteFrameReceived;
            videoManager.ErrorOccurred += OnVideoError;
            videoManager.StreamingStarted += OnStreamingStarted;
            videoManager.StreamingStopped += OnStreamingStopped;

            // Setup call timer
            callTimer = new DispatcherTimer();
            callTimer.Interval = TimeSpan.FromSeconds(1);
            callTimer.Tick += CallTimer_Tick;

            // Start webcam immediately unless delayed
            if (!delayWebcamStart)
            {
                StartWebcamInternal();
            }
            else
            {
                txtLocalStatus.Text = "Waiting for connection...";
                txtCallStatus.Text = "Connecting...";
            }
        }

        private void StartWebcamInternal()
        {
            if (webcamStarted) return;
            
            if (videoManager.StartWebcam())
            {
                txtLocalStatus.Text = "Camera ready";
                webcamStarted = true;
            }
            else
            {
                txtLocalStatus.Text = "Camera not available";
            }
        }

        /// <summary>
        /// Start webcam and call - called when connection is fully established (for acceptor)
        /// </summary>
        public void StartWebcamAndCall()
        {
            Dispatcher.Invoke(() =>
            {
                StartWebcamInternal();
                StartCall();
            });
        }

        public void StartCall()
        {
            isCallActive = true;
            callStartTime = DateTime.Now;
            callTimer.Start();
            txtCallStatus.Text = "Call connected";
        }

        /// <summary>
        /// Refresh the streaming status - called when connection is fully established
        /// </summary>
        public void RefreshStreaming()
        {
            Dispatcher.Invoke(() =>
            {
                if (videoManager.IsActive)
                {
                    txtRemoteStatus.Text = "Streaming active";
                    System.Diagnostics.Debug.WriteLine("[VIDEO WINDOW] Streaming is now active");
                }
            });
        }

        private void OnLocalFrameReceived(Bitmap frame)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    imgLocalVideo.Source = BitmapToImageSource(frame);
                }
                catch { }
                finally
                {
                    frame.Dispose();
                }
            });
        }

        private void OnRemoteFrameReceived(Bitmap frame)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    imgRemoteVideo.Source = BitmapToImageSource(frame);
                    txtRemoteStatus.Text = "Receiving video...";
                }
                catch { }
                finally
                {
                    frame.Dispose();
                }
            });
        }

        private void OnVideoError(string error)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(error, "Video Call Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void OnStreamingStarted()
        {
            Dispatcher.Invoke(() =>
            {
                txtRemoteStatus.Text = "Connected";
            });
        }

        private void OnStreamingStopped()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    txtRemoteStatus.Text = "Connection lost";
                });
            }
            catch
            {
                // Window might be closing
            }
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                
                return bitmapImage;
            }
        }

        private void CallTimer_Tick(object? sender, EventArgs e)
        {
            TimeSpan duration = DateTime.Now - callStartTime;
            txtCallDuration.Text = $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }

        private async void BtnEndCall_Click(object sender, RoutedEventArgs e)
        {
            await EndCall();
        }

        private async System.Threading.Tasks.Task EndCall()
        {
            if (isCallActive)
            {
                // Notify opponent that call ended
                await networkManager.SendCallEndAsync();
            }

            CleanupCall();
            this.Close();
        }

        private void CleanupCall()
        {
            isCallActive = false;
            callTimer.Stop();

            // Unsubscribe from events
            videoManager.LocalFrameReceived -= OnLocalFrameReceived;
            videoManager.RemoteFrameReceived -= OnRemoteFrameReceived;
            videoManager.ErrorOccurred -= OnVideoError;
            videoManager.StreamingStarted -= OnStreamingStarted;
            videoManager.StreamingStopped -= OnStreamingStopped;

            // Stop video
            videoManager.StopWebcam();
            videoManager.StopStreaming();
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isCallActive)
            {
                await networkManager.SendCallEndAsync();
            }
            CleanupCall();
        }
    }
}
