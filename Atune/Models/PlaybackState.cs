using System.Collections.Generic;

namespace Atune.Models
{
    public class PlaybackState
    {
        public List<string> Queue { get; set; } = [];
        public int CurrentIndex { get; set; } = -1;
        public double Position { get; set; } = 0;
    }
}
