using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FPlayer
{
    public class fpPlaylist
    {
        public string name;
        public List<fpPlayItem> list = new List<fpPlayItem>();
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
}
