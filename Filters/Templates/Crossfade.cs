﻿using System;
using System.Linq;
using System.Collections.Generic;
using Hudl.Ffmpeg.Common;
using Hudl.Ffmpeg.Command;
using Hudl.Ffmpeg.Filters.BaseTypes;
using Hudl.Ffmpeg.Resources;
using Hudl.Ffmpeg.Resources.BaseTypes;
using Hudl.Ffmpeg.Settings;
using Hudl.Ffmpeg.Settings.BaseTypes;
using Hudl.Ffmpeg.Templates;

namespace Hudl.Ffmpeg.Filters.Templates
{
    public class Crossfade : Blend, IFilterProcessor
    {
        private const string CrossfadeAlgorithm = "A*(if(gte(T,{0}),1,T/{0}))+B*(1-(if(gte(T,{0}),1,T/{0})))";
        private readonly SettingsCollection _outputSettings = SettingsCollection.ForOutput(
            new OverwriteOutput(), 
            new VCodec(VideoCodecType.Copy));

        public Crossfade(TimeSpan duration, Filterchainv2 resolutionFilterchain)
        {
            Duration = duration;
            Option = BlendVideoOptionType.all_expr;
            ResolutionFilterchain = resolutionFilterchain;
        }

        private TimeSpan _duration; 
        public TimeSpan Duration
        {
            get { return _duration; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value"); 
                }
                _duration = value; 
                Expression = string.Format(CrossfadeAlgorithm, value.TotalSeconds);
            }
        }

        private Filterchainv2 ResolutionFilterchain { get; set; }

        public override TimeSpan? LengthFromInputs(List<CommandResourcev2> resources)
        {
            return Duration;
        }

        public void PrepCommands(Commandv2 command, Filterchainv2 filterchain)
        {
            //verify that we have a resolution filterchain 
            if (ResolutionFilterchain == null)
            {
                throw new InvalidOperationException("A resolution is required for the cross fade command, because of the Blend filter.");
            }

            double resourceToLength;
            double resourceFromLength;
            Filterchainv2 filterchainCutupTransitionTo = null;
            Filterchainv2 filterchainCutupTransitionFrom = null;

            var receiptTo = filterchain.Resources[1];
            var receiptFrom = filterchain.Resources[1];
            var resourceTo = command.ResourcesFromReceipts(receiptTo).First();
            var resourceFrom = command.ResourcesFromReceipts(receiptFrom).First();
            var filterchainCutupBodyTo = command.FilterchainFromReceipt(receiptTo);
            var filterchainCutupBodyFrom = command.FilterchainFromReceipt(receiptFrom);

            //validate that the resources in fact come from the appropriate command
            if (resourceTo == null)
            {
                throw new InvalidOperationException("To resource does not belong to the Command or Command Factory.");
            } 
            if (resourceFrom == null)
            {
                throw new InvalidOperationException("From resource does not belong to the Command or Command Factory.");
            } 

            //get the filterchain body cutup
            if (receiptTo.Type == CommandReceiptType.Stream)
            {
                resourceToLength = filterchainCutupBodyTo.Outputs(command).First().Length.TotalSeconds;
                var hasPreExistingTrim = filterchainCutupBodyTo.Filters.Contains<Trim>();
                if (hasPreExistingTrim)
                {
                    var trimFilter = filterchainCutupBodyTo.Filters.Get<Trim>();
                    if (trimFilter.TimebaseUnit == VideoUnitType.Frames)
                    {
                        filterchainCutupBodyTo = null;
                    }
                }
                else
                {
                    filterchainCutupBodyTo = null;
                }
            }
            else
            {
                resourceToLength = resourceTo.Resource.Length.TotalSeconds;
            }

            if (receiptFrom.Type == CommandReceiptType.Stream)
            {
                resourceFromLength = filterchainCutupBodyFrom.Outputs(command).First().Length.TotalSeconds;
                var hasPreExistingTrim = filterchainCutupBodyFrom.Filters.Contains<Trim>();
                if (hasPreExistingTrim)
                {
                    var trimFilter = filterchainCutupBodyFrom.Filters.Get<Trim>();
                    if (trimFilter.TimebaseUnit == VideoUnitType.Frames)
                    {
                        filterchainCutupBodyFrom = null;
                    }
                }
                else
                {
                    filterchainCutupBodyFrom = null;
                }
            }
            else
            {
                resourceFromLength = resourceFrom.Resource.Length.TotalSeconds;
            }

            //factor the common lengths 
            var durationLength = Duration.TotalSeconds;
            var durationFromEndLength = resourceFromLength - durationLength;

            //an existing trim cannot be located, so a new one is required
            if (filterchainCutupBodyTo == null)
            {
                filterchainCutupBodyTo = new VideoCutTo<TResource>(durationLength, resourceToLength);
            }
            else
            {
                var trimFilter = filterchainCutupBodyTo.Filters.Get<Trim>();
                trimFilter.End -= Duration.TotalSeconds;
                trimFilter.Duration = trimFilter.End - trimFilter.Start;
                filterchainCutupBodyTo.Filters.Merge(trimFilter, FfmpegMergeOptionType.NewWins); 
            }
            filterchainCutupTransitionTo = new VideoCutTo<TResource>(0D, durationLength);

            //an existing trim cannot be located, so a new one is required
            if (filterchainCutupBodyFrom == null)
            {
                filterchainCutupBodyFrom = new VideoCutTo<TResource>(0D, durationFromEndLength);
            }
            else
            {
                var trimFilter = filterchainCutupBodyFrom.Filters.Get<Trim>();
                trimFilter.End -= Duration.TotalSeconds;
                trimFilter.Duration = trimFilter.End - trimFilter.Start;
                filterchainCutupBodyFrom.Filters.Merge(trimFilter, FfmpegMergeOptionType.NewWins);
                durationFromEndLength = trimFilter.End;
                resourceFromLength += Duration.TotalSeconds;
            }
            filterchainCutupTransitionFrom = new VideoCutTo<TResource>(durationFromEndLength, resourceFromLength);

            //reset the filtergraph outputs
            if (resourceTo != null)
            {
                var newInputReceipt = command.RegenerateResourceMap(filterchain.Resources[1]);
                filterchainCutupBodyTo.SetResources(newInputReceipt);
                filterchainCutupTransitionTo.SetResources(newInputReceipt);
            }
            else
            {
                filterchainCutupTransitionTo.SetResources(filterchainCutupBodyTo.Resources.First());
            }
            if (resourceFrom != null)
            {
                var newInputReceipt = command.RegenerateResourceMap(filterchain.Resources[0]);
                filterchainCutupBodyFrom.SetResources(newInputReceipt);
                filterchainCutupTransitionFrom.SetResources(newInputReceipt);
            }
            else
            {
                filterchainCutupTransitionFrom.SetResources(filterchainCutupBodyFrom.Resources.First());
            }

            filterchainCutupBodyFrom.Output.Resource.Map = filterchain.Resources[0].Map;
            filterchainCutupBodyTo.Output.Resource.Map = filterchain.Resources[1].Map;

            command.Filtergraph.Merge(filterchainCutupBodyFrom, FfmpegMergeOptionType.NewWins);
            command.Filtergraph.Add(filterchainCutupTransitionFrom);
            command.Filtergraph.Add(filterchainCutupTransitionTo);
            command.Filtergraph.Merge(filterchainCutupBodyTo, FfmpegMergeOptionType.NewWins);
            
            var transitionToReceipt = new CommandResourceReceipt(command.Parent.Id, command.Id, filterchainCutupTransitionTo.Output.Resource.Map);  
            var transitionFromReceipt = new CommandResourceReceipt(command.Parent.Id, command.Id, filterchainCutupTransitionFrom.Output.Resource.Map);
            var filterchainCopyTo = ResolutionFilterchain.Copy<TResource>();
            var filterchainCopyFrom = ResolutionFilterchain.Copy<TResource>();
            filterchainCopyTo.SetResources(transitionToReceipt);
            filterchainCopyFrom.SetResources(transitionFromReceipt);
            command.Filtergraph.Add(filterchainCopyTo);
            command.Filtergraph.Add(filterchainCopyFrom);

            //assign new receipts to the input filterchain
            var toReceipt = new CommandResourceReceipt(command.Parent.Id, command.Id, filterchainCopyTo.Output.Resource.Map);
            var fromReceipt = new CommandResourceReceipt(command.Parent.Id, command.Id, filterchainCopyFrom.Output.Resource.Map);
            filterchain.SetResources(toReceipt, fromReceipt);
        }
    }
}
