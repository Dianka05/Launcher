using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Media;
using ICSharpCode.SharpZipLib.Zip;
using System.Security.Cryptography;


namespace Sandbox_launcher
{
    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }


    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private void Button_ClickPlayButton(object sender, RoutedEventArgs e)
        {
            CheckForUpdates();
            if (Status == LauncherStatus.ready && File.Exists(gameExe)==false)
            {
                CheckForUpdates();
            }
            else if(File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath);
                Process.Start(startInfo);
                //Close();
            }
        }

        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;


        private LauncherStatus _status;
        internal LauncherStatus Status
        {

            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Play";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Update Failed - Retry";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "Downloading Game";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Downloading Update";
                        break;
                    default:
                        break;
                }
            }

            
        }

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
          
            //CheckForUpdates();


            //rootPath = Directory.GetCurrentDirectory();
            rootPath = Path.Combine(Directory.GetCurrentDirectory(), "GameFiles");
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "OneNight.zip");
            gameExe = Path.Combine(Path.Combine(rootPath, "GameFiles"), "LogJam.exe");
            mediaPlayer.Open(new Uri(Path.Combine(rootPath, "bgmusic.mp3")));
            mediaPlayer.Volume = 0.5; // 50% volume
            mediaPlayer.Play();
            CheckGameFilesExist();
        }

        public void CheckGameFilesExist()
        {
            if (File.Exists(versionFile))
                {
                Console.WriteLine("play");
                PlayButton.Content = "Play";
                VersionText.Text = File.ReadAllText(versionFile).ToString();
            }
            else
            {
                Console.WriteLine("Down");
                PlayButton.Content = "Downloads Game";
            }
        }

        private void CheckForUpdates()
        {

            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();
                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&confirm=no_antivirus&id=1K1bjxOO1vRI-qbElJJiXYET47LbkIMyc"));
                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            LabelProgress.Visibility = Visibility.Visible;
            ProgressBar.Visibility = Visibility.Visible;
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;

                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&confirm=no_antivirus&id=1K1bjxOO1vRI-qbElJJiXYET47LbkIMyc"));
                }

                
                webClient.DownloadProgressChanged += (s, e) =>
                {
                    LabelProgress.Text = $"Downloading: {e.ProgressPercentage}% ({ ((double)e.BytesReceived / 1048576).ToString("#.#")} MB)";
                    ProgressBar.Value = e.ProgressPercentage;
                };

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri("https://drive.google.com/uc?export=download&confirm=no_antivirus&id=1Jh0Dyjdi5GsnFoh0KsnkPxRdzFnIYR4O"), gameZip, _onlineVersion);//1
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game files: {ex}");
            }


           
        }
        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();

                string targetDirectory = Path.Combine(rootPath, "GameFiles");

                string targetFilePath = Path.Combine(targetDirectory, "LogJam.exe");

                if (!File.Exists(targetFilePath))
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(gameZip, targetDirectory, Encoding.Default);
                    File.Delete(gameZip);

                    File.WriteAllText(versionFile, onlineVersion);
                    VersionText.Text = onlineVersion;

                    Status = LauncherStatus.ready;

                    LabelProgress.Visibility = Visibility.Hidden;
                    ProgressBar.Visibility = Visibility.Hidden;
                }
                else
                {
                    DeleteCurrentGameFiles();
                    System.IO.Compression.ZipFile.ExtractToDirectory(gameZip, targetDirectory, Encoding.Default);
                    File.Delete(gameZip);

                    File.WriteAllText(versionFile, onlineVersion);
                    VersionText.Text = onlineVersion;

                    Status = LauncherStatus.ready;

                    LabelProgress.Visibility = Visibility.Hidden;
                    ProgressBar.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Ошибка при завершении загрузки: {ex}");
            }
        }

        //private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        //{
        //    try
        //    {
        //        string onlineVersion = ((Version)e.UserState).ToString();

        //        //ZipFile.ExtractToDirectory(gameZip, rootPath, Encoding.Default);
        //        //File.Delete(gameZip);

        //        string targetDirectory = Path.Combine(rootPath, "GameFiles");
        //        ZipFile.ExtractToDirectory(gameZip, rootPath, Encoding.Default);
        //        File.Delete(gameZip);

        //        File.WriteAllText(versionFile, onlineVersion);
        //        VersionText.Text = onlineVersion;

        //        Status = LauncherStatus.ready;

        //        LabelProgress.Visibility = Visibility.Hidden;
        //        ProgressBar.Visibility = Visibility.Hidden;

        //    }
        //    catch (Exception ex)
        //    {
        //        Status = LauncherStatus.failed;
        //        MessageBox.Show($"Error finishing download: {ex}");
        //    }
        //}
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void VersionText_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (AudioBtn.Content.ToString() == "Audio: ON")
            {
                AudioBtn.Content = "Audio: OFF";
                mediaPlayer.Stop();
                //CheckForUpdates();
            }
            else if (AudioBtn.Content.ToString() == "Audio: OFF")
            {
                AudioBtn.Content = "Audio: ON";
                mediaPlayer.Play();
            }
        }

        public void DeleteCurrentGameFiles()
        {
            if (File.Exists(gameExe) && File.Exists(versionFile))
            {
                Directory.Delete(Path.Combine(rootPath, "GameFiles"), true);
                File.Delete(Path.Combine(rootPath, "Version.txt"));
                VersionText.Text = "0.0.0";
            }
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            DeleteCurrentGameFiles();
            CheckGameFilesExist();
        }

    }


    struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }
        internal Version(string _version)
        {
            string[] versionStrings = _version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }

        internal bool IsDifferentThan(Version _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
