using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace TwitchDownloaderCore.Options
{
    public class CombinerOptions
    {
        public string InputFile1 { get; set; }
        public string InputFile2 { get; set; }
        public string OutputFile { get; set; }
        public bool Timestamp { get; set; }
        public int Framerate { get; set; }
        public string InputArgs { get; set; }
        public string OutputArgs { get; set; }
        public string FfmpegPath { get; set; }
        public string TempFolder { get; set; }
        public int RenderThreads { get; set; } = 1;
        public bool LogFfmpegOutput { get; set; } = false;
    }
}
