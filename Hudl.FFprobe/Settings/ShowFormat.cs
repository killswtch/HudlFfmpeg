﻿using Hudl.FFmpeg.Settings.Attributes;
using Hudl.FFmpeg.Settings.Interfaces;

namespace Hudl.FFprobe.Settings
{
    [Setting(Name = "show_format", IsParameterless = true)]
    internal class ShowFormat : ISetting
    {
    }
}
