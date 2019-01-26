using System.Collections.Generic;

namespace AutoDJ.Models
{
    public class Mode
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Dictionary<string, int> Config { get; set; }
    }
}
