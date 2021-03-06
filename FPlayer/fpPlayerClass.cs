﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FPlayer
{
    public class FPlayerDataBase
    {
        public List<fpPlaylist> playlists;
        public int playlistIndex = 0;
        [JsonIgnore]
        public fpPlaylist currentPlaylist
        {
            get
            {
                return playlists[playlistIndex];
            }
        }
        [JsonIgnore]
        public fpPlayItem currentPlayitem
        {
            get
            {
                return currentPlaylist.list[playitemIndex];
            }
        }
        [JsonIgnore]
        public fpPlayItem[] randomList;

        public float volume = 1.0F;
        public int playitemIndex = 0;
        public PlayerLoopMode loopMode;
        private PlayerRandomMode randomMode;
        public bool autoPause = false;

        public List<PauseIgnore> ignores;
        [JsonIgnore]
        public List<PauseIgnore> recentlyIgnores = new List<PauseIgnore>();

        public void checkDB()
        {
            if (ignores == null)
                ignores = new List<PauseIgnore>();
        }

        public PlayerRandomMode RandomMode
        {
            get { return randomMode; }
            set
            {
                randomMode = value;
                if (value == PlayerRandomMode.Random)
                {
                    newRandomList();
                }
            }
        }
        public void newRandomList()
        {
            randomList = currentPlaylist.list.ToArray<fpPlayItem>();
            Random randomer = new Random();
            for (int i = 0; i < randomList.Length; i++)
            {
                int randomIndex = randomer.Next(randomList.Length);
                fpPlayItem item1 = randomList[i];
                fpPlayItem item2 = randomList[randomIndex];
                randomList[i] = item2;
                randomList[randomIndex] = item1;
            }
        }

        public fpPlayItem getCurrentItem()
        {
            if (RandomMode == PlayerRandomMode.Random)
            {
                return randomList[playitemIndex];
            }
            else
            {
                return currentPlaylist.list[playitemIndex];
            }
        }
    }
    public class fpPlaylist
    {
        public string name;
        public List<fpPlayItem> list = new List<fpPlayItem>();

        public override string ToString()
        {
            return name;
        }
    }
    public class fpPlayItem
    {
        public string path;

        [JsonIgnore]
        public string Name { get; set; }
        [JsonIgnore]
        public string Artist { get; set; }
        [JsonIgnore]
        public string Album { get; set; }
    }
    public enum PlayerLoopMode
    {
        NoLoop = 0,
        Loop,
        SingleLoop
    }
    public enum PlayerRandomMode
    {
        Sequential = 0,
        Random
    }
    public enum PlayerState
    {
        Stop = 0,
        Pause,
        Playing,
        AutoPause,
        KeyboardPause
    }
    public class PauseIgnore
    {
        public string title;
        public string path;
        public bool enable;
    }
}
