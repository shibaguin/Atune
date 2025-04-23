using System.Collections.Generic;

namespace Atune.Models
{
    public class PlaybackState
    {
        public List<string> Queue { get; set; } = new List<string>();
        public int CurrentIndex { get; set; } = -1;
        public double Position { get; set; } = 0;
    }
} 
