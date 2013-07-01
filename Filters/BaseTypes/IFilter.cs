﻿using System;
using System.Collections.Generic;
using Hudl.Ffmpeg.Command;
using Hudl.Ffmpeg.Resources.BaseTypes;

namespace Hudl.Ffmpeg.Filters.BaseTypes
{
    /// <summary>
    /// representation of a simple filt 
    /// </summary>
    public interface IFilter
    {
        /// <summary>
        /// the command name for the affect
        /// </summary>
        string Type { get; }

        /// <summary>
        /// maximum number of inputs that the filter can support
        /// </summary>
        int MaxInputs { get; }

        /// <summary>
        /// the difference the fitler will make on the output video length
        /// </summary>
        /// <returns>Null indicates that the length difference does not apply</returns>
        TimeSpan? LengthDifference { get; }

        /// <summary>
        /// the length override property is an override to the input video length
        /// </summary>
        /// <returns>Null indicates that the length difference does not apply</returns>
        TimeSpan? LengthOverride { get; }

        /// <summary>
        /// the length override function, overrided when a fitler requires a length change of output calculated from the resources.
        /// </summary>
        /// <returns>Null indicates that the length difference does not apply</returns>
        TimeSpan? LengthFromInputs(List<CommandResource<IResource>> resources);

        /// <summary>
        /// builds the command necessary to complete the effect
        /// </summary>
        string ToString();

        /// <summary>
        /// sets up the filter based on the settings in the filterchain
        /// </summary>
        void Setup<TOutput>(Command<TOutput> command, Filterchain<TOutput> filterchain)
            where TOutput : IResource;
    }
}
