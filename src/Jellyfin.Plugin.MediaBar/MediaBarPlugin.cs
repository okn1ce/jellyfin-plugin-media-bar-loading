using System.Reflection;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.FileTransformation.Controller;
using Jellyfin.Plugin.FileTransformation.Infrastructure;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.MediaBar;

public class MediaBarPlugin : BasePlugin<BasePluginConfiguration>
{
    public override Guid Id => Guid.Parse("08f615ea-2107-4f04-89cc-091035f54448");

    public override string Name => "Media Bar";
    
    public MediaBarPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IServiceProvider serviceProvider) : base(applicationPaths, xmlSerializer)
    {
        IWebFileTransformationWriteService webFileTransformationWriteService = serviceProvider.GetRequiredService<IWebFileTransformationWriteService>();
        
        webFileTransformationWriteService.AddTransformation("index.html", IndexHtml_Transformation);
        webFileTransformationWriteService.AddTransformation("home-html\\.[a-zA-z0-9]+\\.chunk\\.js", HomeHtmlChunk_Transformation);
    }

    private void IndexHtml_Transformation(string path, Stream contents)
    {
        Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{GetType().Namespace}.Inject.index.html");
        using TextReader reader = new StreamReader(stream);
        
        using var textReader = new StreamReader(contents, null, true, -1, true);
        var text = textReader.ReadToEnd();

        var regex = Regex.Replace(text, "(</body>)", $"{reader.ReadToEnd()}$1");
        contents.Seek(0, SeekOrigin.Begin);
        
        using var textWriter = new StreamWriter(contents, null, -1, true);
        textWriter.Write(regex);
    }
    
    private void HomeHtmlChunk_Transformation(string path, Stream contents)
    {
        Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{GetType().Namespace}.Inject.home-html.chunk.js");
        using TextReader reader = new StreamReader(stream);
        
        using var textReader = new StreamReader(contents, null, true, -1, true);
        var text = textReader.ReadToEnd();

        var regex = Regex.Replace(text, "(id=\"homeTab\" data-index=\"0\">)", $"$1{reader.ReadToEnd()}");
        contents.Seek(0, SeekOrigin.Begin);
        
        using var textWriter = new StreamWriter(contents, null, -1, true);
        textWriter.Write(regex);
    }
}