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
        
        //private AudioFileReader audioFile;
        string DBPath = "playlistDB.json";
        List<fpPlaylist> playlists;
        int playlistIndex = 0;
        fpPlaylist playlist;
        int playitemIndex = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (playlistIndex >= playlists.Count) { return; }
            fpPlaylist playlist = playlists[playlistIndex];
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
            if (playlists.Count > 0)
            {
                playlist = playlists[0];
                for (int index = 0; index < playlist.list.Count; index++)
                {
                    fpPlayItem playitem = playlist.list[index];
                    if (File.Exists(playitem.path))
                    {
                        Track track = new Track(playitem.path);
                        playitem.Name = track.Title;
                        playitem.Artist = track.Artist;
                        playitem.Album = track.Album;
                    }
                    else
                    {
                        playlist.list.Remove(playitem);
                        continue;
                    }
                }
                listItems.ItemsSource = playlist.list;
            }
            playlistIndex = 0;

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
                                        foundOtherVolume = true;
                                        break;
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
                    });

                }, null, 0, 100);

        }


        private void Window_Closed(object sender, EventArgs e)
        {
            writeDB();
        }
        void initPlaylist()
        {
            playlists = new List<fpPlaylist>();
            fpPlaylist playlist = new fpPlaylist()
            {
                name = "播放清單"
            };
            playlist.list.Add(new fpPlayItem() { path = @"24. Life Will Change -instrumental version-.flac" });
            playlists.Add(playlist);

            writeDB();

        }
        void writeDB()
        {
            using (StreamWriter writer = new StreamWriter(DBPath, false))
            {
                string jsonString = JsonConvert.SerializeObject(playlists);
                writer.Write(jsonString);
            }
        }
        void readDB()
        {
            using (StreamReader reader = new StreamReader(DBPath))
            {
                string jsonString = reader.ReadToEnd();
                playlists = JsonConvert.DeserializeObject<List<fpPlaylist>>(jsonString);
            }
        }
        void loadPlayerItem()
        { 
            if (audioPlayerItem != null)
            {
                audioPlayer.Stop();
                audioPlayerItem.Close();
            }
            fpPlayItem playitem = playlist.list[playitemIndex];
            audioPlayerItem = new AudioFileReader(playitem.path);
            sliderProgress.Maximum = audioPlayerItem.Length;
            playerItemTrack = new Track(playitem.path);
            listItems.SelectedIndex = playitemIndex;
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
            playitemIndex--;
            if (playitemIndex < 0)
            {
                playitemIndex = playlist.list.Count - 1;
            }
            loadPlayerItem();
            play();
        }
        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            playitemIndex++;
            if (playitemIndex >= playlist.list.Count)
            {
                playitemIndex = 0;
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

        }

        private void BtnLoop_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SliderProgress_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            audioPlayerItem.Position = (long)sliderProgress.Value;
        }

        private void ListItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (playitemIndex != listItems.SelectedIndex)
            {
                playitemIndex = listItems.SelectedIndex;
                audioPlayer.Stop();
                loadPlayerItem();
                play();
            }
        }
    }
}
