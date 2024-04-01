using System;
using BepInEx.Configuration;
using LethalConfig.ConfigItems.Options;
using LethalConfig.Mods;
using UnityEngine;

namespace LethalConfig.ConfigItems
{
    public abstract class BaseConfigItem
    {
        private object _currentBoxedValue;

        internal BaseConfigItem(BaseOptions options) : this(null, options)
        {
        }

        internal BaseConfigItem(ConfigEntryBase configEntry, BaseOptions options)
        {
            BaseConfigEntry = configEntry;
            Options = options;
            CurrentBoxedValue = OriginalBoxedValue;
        }

        internal ConfigEntryBase BaseConfigEntry { get; }
        internal BaseOptions Options { get; }
        internal bool RequiresRestart => Options.RequiresRestart;
        internal Mod Owner { get; set; }
        internal bool IsAutoGenerated { get; set; }

        private string UnderlyingSection => BaseConfigEntry?.Definition.Section ?? string.Empty;
        private string UnderlyingName => BaseConfigEntry?.Definition.Key ?? string.Empty;
        private string UnderlyingDescription => BaseConfigEntry?.Description.Description ?? string.Empty;

        internal string Section => Options.Section ?? UnderlyingSection;
        internal string Name => Options.Name ?? UnderlyingName;
        internal string Description => Options.Description ?? UnderlyingDescription;

        private object OriginalBoxedValue => BaseConfigEntry?.BoxedValue;
        internal object BoxedDefaultValue => BaseConfigEntry?.DefaultValue;

        internal object CurrentBoxedValue
        {
            get => _currentBoxedValue;
            set
            {
                _currentBoxedValue = value;
                OnCurrentValueChanged?.Invoke();
            }
        }

        internal bool HasValueChanged => !CurrentBoxedValue?.Equals(OriginalBoxedValue) ?? false;

        internal abstract GameObject CreateGameObjectForConfig();

        internal event CurrentValueChangedHandler OnCurrentValueChanged;

        public void ApplyChanges()
        {
            if (BaseConfigEntry == null) return;
            BaseConfigEntry.BoxedValue = CurrentBoxedValue;
        }

        public void CancelChanges()
        {
            CurrentBoxedValue = OriginalBoxedValue;
        }

        public void ChangeToDefault()
        {
            CurrentBoxedValue = BoxedDefaultValue;
        }

        internal virtual bool IsSameConfig(BaseConfigItem configItem)
        {
            var isSameSection = configItem.UnderlyingSection == UnderlyingSection;
            var isSameKey = configItem.UnderlyingName == UnderlyingName;
            var isSameMod = configItem.Owner.ModInfo.Guid == Owner.ModInfo.Guid;
            return isSameSection && isSameKey && isSameMod;
        }

        internal delegate void CurrentValueChangedHandler();
    }

    public abstract class BaseValueConfigItem<T> : BaseConfigItem
    {
        internal BaseValueConfigItem(ConfigEntry<T> configEntry, BaseOptions options) : base(configEntry, options)
        {
            CurrentValue = OriginalValue;
            if (ConfigEntry != null)
                ConfigEntry.SettingChanged += OnUnderlyingEntryChanged;
        }

        private ConfigEntry<T> ConfigEntry => (ConfigEntry<T>)BaseConfigEntry;

        internal T CurrentValue
        {
            get => (T)CurrentBoxedValue;
            set => CurrentBoxedValue = value;
        }

        private T OriginalValue => ConfigEntry.Value;
        internal T DefaultValue => (T)ConfigEntry.DefaultValue;

        ~BaseValueConfigItem()
        {
            if (ConfigEntry != null)
                ConfigEntry.SettingChanged -= OnUnderlyingEntryChanged;
        }

        private void OnUnderlyingEntryChanged(object sender, EventArgs args)
        {
            CurrentValue = OriginalValue;
        }

        public override string ToString()
        {
            return $"{Owner.ModInfo.Name}/{ConfigEntry.Definition.Section}/{ConfigEntry.Definition.Key}";
        }
    }
}