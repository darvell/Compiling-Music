using System;
using System.Collections.Generic;
using System.Text;

namespace CompilingMusic
{
    class Settings
    {
        public string bassUser { get; set; }
        public string bassCode { get; set; }
        public bool setMode { get; set; }
        public string setDirectory { get; set; }
        public string randomCompileDirectory { get; set; }
        public string randomSuccessDirectory { get; set; }
        public string randomFailDirectory { get; set; }
        public bool STFU { get; set; }
    }

    class Set
    {
        public string basePath { get; set; }
        public string[] compileSong { get; set; }
        public string[] failSong { get; set; }
        public string[] successSong { get; set; }
    }


}
