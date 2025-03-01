using System.Reflection;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.MediaBar.Model;

namespace Jellyfin.Plugin.MediaBar.Helpers
{
    public static class TransformationPatches
    {
        public static string IndexHtml(PatchRequestPayload payload)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(MediaBarPlugin).Namespace}.Inject.index.html")!;
            using TextReader reader = new StreamReader(stream);

            string regex = Regex.Replace(payload.Contents!, "(</body>)", $"{reader.ReadToEnd()}$1");
            
            return regex;
        }

        public static string HomeHtmlChunk(PatchRequestPayload payload)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(MediaBarPlugin).Namespace}.Inject.home-html.chunk.js")!;
            using TextReader reader = new StreamReader(stream);

            string regex = Regex.Replace(payload.Contents!, "(id=\"homeTab\" data-index=\"0\">)", $"$1{reader.ReadToEnd()}");
            
            return regex;
        }

        public static string MainBundle(PatchRequestPayload payload)
        {
            string replacementText =
                "window.PlaybackManager=this.playbackManager;console.log(\"PlaybackManager is now globally available:\",window.PlaybackManager);";
            
            string regex = Regex.Replace(payload.Contents!, @"(this\.playbackManager=e,)", $"$1{replacementText}");

            return regex;
        }
    }
}