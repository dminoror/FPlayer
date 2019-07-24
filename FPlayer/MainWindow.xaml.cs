using ATL;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FPlayer
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        private WaveOutEvent audioPlayer;
        AudioFileReader audioPlayerItem;
        Track playerItemTrack;

        MMDeviceEnumerator devEnum = new MMDeviceEnumerator();
        bool autoPause = false;

        Timer timerProgress;
        Timer timerVolumeListener;
        
        string DBPath = "playlistDB.json";
        FPlayerDataBase playerDB;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (playerDB.playlistIndex >= playerDB.playlists.Count) { return; }
            fpPlaylist playlist = playerDB.playlists[playerDB.playlistIndex];
            int savedCount = playlist.list.Count;
            var paths = ((System.Array)e.Data.GetData(DataFormats.FileDrop));
            if (paths == null) { return; }
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths.GetValue(i).ToString();
                Track track = new Track(path);
                if (track.Duration > 0)
                {
                    fpPlayItem item = new fpPlayItem()
                    {
                        path = path,
                        Name = track.Title,
                        Artist = track.Artist,
                        Album = track.Album
                    };
                    if (!playlist.list.Any(playItem => playItem.path == item.path))
                    {
                        playlist.list.Add(item);
                    }
                    listItems.Items.Refresh();
                }
            }
            if (playlist.list.Count != savedCount)
            {
                playerDB.newRandomList();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(DBPath))
            {
                readDB();
            }
            else
            {
                initPlaylist();
            }
            playerDB.checkDB();
            if (playerDB.playlists.Count > 0)
            {
                playerDB.playlist = playerDB.playlists[0];
                for (int index = 0; index < playerDB.playlist.list.Count; index++)
                {
                    fpPlayItem playitem = playerDB.playlist.list[index];
                    if (File.Exists(playitem.path))
                    {
                        Track track = new Track(playitem.path);
                        playitem.Name = track.Title;
                        playitem.Artist = track.Artist;
                        playitem.Album = track.Album;
                    }
                    else
                    {
                        playerDB.playlist.list.Remove(playitem);
                        continue;
                    }
                }
                listItems.ItemsSource = playerDB.playlist.list;
            }
            playerDB.playlistIndex = 0;

            if (audioPlayer == null)
            {
                audioPlayer = new WaveOutEvent();
                //audioPlayer.PlaybackStopped += AudioPlayer_PlaybackStopped;
            }

            timerProgress = new Timer(
                (ignore) =>
                {
                    if (audioPlayerItem != null)
                    {
                        Dispatcher.Invoke((Action)delegate () {
                            sliderProgress.Value = audioPlayerItem.Position;
                            if (audioPlayerItem.Length - audioPlayerItem.Position < 1)
                            {
                                BtnNext_Click(null, null);
                            }
                        });
                    }
                    
                }, null, 0, 1000);
            timerVolumeListener = new Timer(
                (ignore) =>
                {
                    Dispatcher.Invoke((Action)delegate () {
                        using (MMDevice defaultDevice = devEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                        {
                            bool foundOtherVolume = false;
                            for (int index = 0; index < defaultDevice.AudioSessionManager.Sessions.Count; index++)
                            {
                                var session = defaultDevice.AudioSessionManager.Sessions[index];
                                if (!session.GetSessionIdentifier.Contains("FPlayer.exe"))
                                {
                                    if (session.AudioMeterInformation.MasterPeakValue > 0.1)
                                    {
                                        var ignores = playerDB.ignores.Where(pauseIgnore => pauseIgnore.path == session.GetSessionIdentifier);
                                        bool foundIgnore = false;
                                        bool existIgnore = ignores.Count() > 0;
                                        foreach (PauseIgnore pauseIgnore in ignores)
                                        {
                                            if (pauseIgnore.enable)
                                            {
                                                foundIgnore = true;
                                            }
                                        }
                                        if (!foundIgnore)
                                        {
                                            foundOtherVolume = true;
                                        }
                                        if (!existIgnore)
                                        {
                                            bool foundRecently = playerDB.recentlyIgnores.Where(pauseIgnore => pauseIgnore.path == session.GetSessionIdentifier).Count() > 0;
                                            if (!foundRecently)
                                            {
                                                string sessionId;
                                                if (session.IconPath.Contains(@"System32\AudioSrv.Dll"))
                                                {
                                                    sessionId = "系統音效";
                                                }
                                                else
                                                {
                                                    var sessionSplit = session.GetSessionIdentifier.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
                                                    if (sessionSplit.Length > 0)
                                                        sessionId = sessionSplit.Last().Split(new string[] { @"%b" }, StringSplitOptions.RemoveEmptyEntries).First();
                                                    else
                                                        sessionId = session.GetSessionIdentifier;
                                                }
                                                PauseIgnore pauseIgnore = new PauseIgnore()
                                                {
                                                    title = sessionId,
                                                    path = session.GetSessionIdentifier,
                                                    enable = false
                                                };
                                                playerDB.recentlyIgnores.Add(pauseIgnore);
                                            }
                                        }
                                    }
                                }
                                if (foundOtherVolume)
                                {
                                    if (audioPlayer.PlaybackState == PlaybackState.Playing)
                                    {
                                        autoPause = true;
                                        audioPlayer.Pause();
                                    }
                                }
                                else
                                {
                                    if (audioPlayer.PlaybackState == PlaybackState.Paused &&
                                        audioPlayerItem != null &&
                                        autoPause)
                                    {
                                        autoPause = false;
                                        audioPlayer.Play();
                                    }
                                }
                            }
                        }
                    });

                }, null, 0, 100);
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            writeDB();
        }
        void initPlaylist()
        {
            playerDB = new FPlayerDataBase();
            playerDB.playlists = new List<fpPlaylist>();
            fpPlaylist playlist = new fpPlaylist()
            {
                name = "播放清單"
            };
            playlist.list.Add(new fpPlayItem() { path = @"24. Life Will Change -instrumental version-.m4a" });
            playerDB.playlists.Add(playlist);
            playerDB.loopMode = PlayerLoopMode.Loop;
            playerDB.RandomMode = PlayerRandomMode.Random;
            writeDB();

        }
        void writeDB()
        {
            using (StreamWriter writer = new StreamWriter(DBPath, false))
            {
                string jsonString = JsonConvert.SerializeObject(playerDB);
                writer.Write(jsonString);
            }
        }
        void readDB()
        {
            using (StreamReader reader = new StreamReader(DBPath))
            {
                string jsonString = reader.ReadToEnd();
                playerDB = JsonConvert.DeserializeObject<FPlayerDataBase>(jsonString);
            }
        }
        void loadPlayerItem()
        { 
            if (audioPlayerItem != null)
            {
                audioPlayer.Stop();
                audioPlayerItem.Close();
            }
            fpPlayItem playitem;
            if (playerDB.RandomMode == PlayerRandomMode.Sequential)
            {
                playitem = playerDB.playlist.list[playerDB.playitemIndex];
                listItems.SelectedIndex = playerDB.playitemIndex;
            }
            else
            {
                playitem = playerDB.randomList[playerDB.playitemIndex];
                int realIndex = playerDB.playlist.list.IndexOf(playitem);
                listItems.SelectedIndex = realIndex;
            }
            audioPlayerItem = new AudioFileReader(playitem.path);
            sliderProgress.Maximum = audioPlayerItem.Length;
            playerItemTrack = new Track(playitem.path);
            tbTitle.Text = playerItemTrack.Title;
            tbAlbum.Text = playerItemTrack.Album;
            tbArtist.Text = playerItemTrack.Artist;
            if (playerItemTrack.EmbeddedPictures.Count > 0)
            {
                PictureInfo pic = playerItemTrack.EmbeddedPictures[0];
                using (var ms = new MemoryStream(pic.PictureData))
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = ms;
                    bi.EndInit();
                    imageCover.Source = bi;
                }
             }
            audioPlayer.Init(audioPlayerItem);
        }
        void play()
        {
            btnPausePlay.Content = "暫停";
            audioPlayer.Play();
        }
        void pause()
        {
            btnPausePlay.Content = "播放";
            audioPlayer.Pause();
        }
        void setLoopMode(PlayerLoopMode loopMode)
        {
            playerDB.loopMode = loopMode;
            switch(loopMode)
            {
                case PlayerLoopMode.NoLoop:
                    btnLoop.Content = "不循環";
                    break;
                case PlayerLoopMode.Loop:
                    btnLoop.Content = "循環";
                    break;
                case PlayerLoopMode.SingleLoop:
                    btnLoop.Content = "單曲";
                    break;
            }
        }
        void setRandomMode(PlayerRandomMode randomMode)
        {
            playerDB.RandomMode = randomMode;
            switch(randomMode)
            {
                case PlayerRandomMode.Sequential:
                    btnRandom.Content = "循序";
                    break;
                case PlayerRandomMode.Random:
                    btnRandom.Content = "隨機";
                    break;
            }
        }
        /*
        private void AudioPlayer_PlaybackStopped(object sender, StoppedEventArgs e)
        {
        }
        */

        private void BtnPausePlay_Click(object sender, RoutedEventArgs e)
        {
            if (audioPlayer.PlaybackState == PlaybackState.Playing)
            {
                pause();
            }
            else
            {
                if (audioPlayerItem == null)
                {
                    loadPlayerItem();
                }
                play();
            }
        }
        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            playerDB.playitemIndex--;
            if (playerDB.playitemIndex < 0)
            {
                playerDB.playitemIndex = playerDB.playlist.list.Count - 1;
            }
            loadPlayerItem();
            play();
        }
        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            playerDB.playitemIndex++;
            if (playerDB.playitemIndex >= playerDB.playlist.list.Count)
            {
                playerDB.playitemIndex = 0;
            }
            loadPlayerItem();
            play();
        }
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            btnPausePlay.Content = "播放";
            audioPlayer.Stop();
        }

        private void BtnRandom_Click(object sender, RoutedEventArgs e)
        {
            setRandomMode((PlayerRandomMode)(((int)playerDB.RandomMode + 1) % 2));
        }

        private void BtnLoop_Click(object sender, RoutedEventArgs e)
        {
            setLoopMode((PlayerLoopMode)(((int)playerDB.loopMode + 1) % 3));
        }
        private void btnAutoPauseOption_Clicked(object sender, RoutedEventArgs e)
        {
            AutoPauseOption page = new AutoPauseOption(playerDB);
            page.ShowDialog();
        }

        private void SliderProgress_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            audioPlayerItem.Position = (long)sliderProgress.Value;
        }

        private void ListItems_DoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (playerDB.RandomMode == PlayerRandomMode.Random)
            {
                fpPlayItem playitem = playerDB.playlist.list[listItems.SelectedIndex];
                playerDB.playitemIndex = Array.IndexOf(playerDB.randomList, playitem);
            }
            else
            {
                playerDB.playitemIndex = listItems.SelectedIndex;
            }
            audioPlayer.Stop();
            loadPlayerItem();
            play();
        }
    }
}
