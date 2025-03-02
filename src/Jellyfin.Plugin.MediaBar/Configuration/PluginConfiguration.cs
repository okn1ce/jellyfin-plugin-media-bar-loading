using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MediaBar.Configuration
{
    public enum MediaBarState
    {
        Disabled,
        Enabled,
    }
    
    public class PluginConfiguration : BasePluginConfiguration
    {
        public MediaBarState Enabled { get; set; } = MediaBarState.Enabled;

        public bool UseAvatarsFile { get; set; } = true;

        public string AvatarsPlaylist { get; set; } = string.Empty;
    }
}