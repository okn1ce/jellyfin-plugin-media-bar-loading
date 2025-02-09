using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.MediaBar
{
    public class MediaBarPlugin : BasePlugin<BasePluginConfiguration>
    {
        public override Guid Id => Guid.Parse("08f615ea-2107-4f04-89cc-091035f54448");

        public override string Name => "Media Bar";
    
        public MediaBarPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IServiceProvider serviceProvider) : base(applicationPaths, xmlSerializer)
        {
        }
    }
}