﻿using System;
using System.Linq;
using System.Collections.Generic;
using Hudl.Ffmpeg.Common;

namespace Hudl.Ffmpeg.Settings.BaseTypes
{
    /// <summary>
    /// A series of Settings that apply to a single resource type.
    /// </summary>
    public class SettingsCollection
    {
        internal SettingsCollection()
            : this(SettingsCollectionResourceTypes.Input)
        {
        }
        internal SettingsCollection(params ISetting[] settings)
            : this(SettingsCollectionResourceTypes.Input, settings)
        {
        }
        internal SettingsCollection(SettingsCollectionResourceTypes type, params ISetting[] settings)
        {
            Type = type;
            SettingsList = new List<ISetting>(settings);   
        }

        public IReadOnlyList<ISetting> Items { get { return SettingsList.AsReadOnly(); } }

        public SettingsCollectionResourceTypes Type { get; protected set; } 

        /// <summary>
        /// returns a new settings collection instance for input collections
        /// </summary>
        public static SettingsCollection ForInput(params ISetting[] settings)
        {
            return SettingsCollection.ForInput(SettingsCollectionResourceTypes.Input, settings);
        }

        /// <summary>
        /// returns a new settins collection instance for output collections
        /// </summary>
        public static SettingsCollection ForOutput(params ISetting[] settings)
        {
            return SettingsCollection.ForInput(SettingsCollectionResourceTypes.Output, settings);
        }

        /// <summary>
        /// adds the given Setting to the SettingsCollection
        /// </summary>
        /// <typeparam name="TSetting">the generic type of the Setting</typeparam>
        /// <param name="setting">the Setting to be added to the SettingsCollection</param>
        public SettingsCollection Add<TSetting>(TSetting setting)
            where TSetting : ISetting
        {
            if (setting == null)
            {
                throw new ArgumentNullException("setting");
            }
            if (Contains<TSetting>())
            {
                throw new ArgumentException(string.Format("The SettingsCollection already contains a type of {0}.", typeof(TSetting).Name));
            }
            if (Type != SettingsCollectionResourceTypes.Any &&
                !Validate.SettingsFor<TSetting>(Type))
            {
                throw new ArgumentException(string.Format("The SettingsCollection is restricted only to {0} settings.", typeof(TSetting).Name));
            }

            SettingsList.Add(setting);
            return this;
        }

        /// <summary>
        /// adds the given SettingsCollection range to the SettingsCollection
        /// </summary>
        /// <param name="settings">the SettingsCollection to be added  to be added to the SettingsCollection</param>
        public SettingsCollection AddRange(SettingsCollection settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            settings.SettingsList.ForEach(s => Add(s));
            return this;
        }

        /// <summary>
        /// merges the current setting into the set based on the merge option type
        /// </summary>
        public SettingsCollection Merge<TSetting>(TSetting setting, FfmpegMergeOptionTypes option)
            where TSetting : ISetting
        {
            if (setting == null)
            {
                throw new ArgumentNullException("setting");
            }

            var alreadyContainsSetting = Contains<TSetting>(); 
            if (alreadyContainsSetting)
            {
                if (option == FfmpegMergeOptionTypes.NewWins)
                {
                    Remove<TSetting>();
                    Add(setting);
                }
            }
            else
            {
                Add(setting);
            }

            return this;
        }

        /// <summary>
        /// merges the current SettingsCollection into the set based on the merge option type.
        /// </summary>
        public SettingsCollection Merge(SettingsCollection settings, FfmpegMergeOptionTypes option)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            settings.SettingsList.ForEach(s => Merge(s, option));
            return this;
        }
        
        /// <summary>
        /// determines if the settings collection already contains this setting type
        /// </summary>
        public bool Contains<TSetting>()
            where TSetting : ISetting
        {
            return (SettingsList.Count(s => s is TSetting) > 0);
        }

        /// <summary>
        /// removes the Setting at the given index from the SettingsCollection
        /// </summary>
        /// <param name="index">the index of the desired Setting to be removed from the SettingsCollection</param>
        public SettingsCollection Remove(int index)
        {
            SettingsList.RemoveAt(index);
            return this;
        }

        /// <summary>
        /// removes the specified setting type from the SettingsCollection
        /// </summary>
        /// <typeparam name="TSetting">the settings type that is to be removed</typeparam>
        public SettingsCollection Remove<TSetting>()
            where TSetting : ISetting
        {
            SettingsList.RemoveAll(s => s is TSetting);
            return this;
        }

        /// <summary>
        /// removes all the Setting matching the provided criteria
        /// </summary>
        /// <param name="pred">the predicate of required criteria</param>
        public SettingsCollection RemoveAll(Predicate<ISetting> pred)
        {
            SettingsList.RemoveAll(pred);
            return this;
        }

        #region Internals
        internal List<ISetting> SettingsList { get; set; }
        #endregion 
    }
}
