using System.Reflection;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.MediaBar.Model;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.MediaBar.Controllers
{
    [Route("[controller]")]
    public class MediaBarController : ControllerBase
    {
        [HttpGet("{file}")]
        public ActionResult GetFile([FromRoute] string file)
        {
            Stream fileStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Jellyfin.Plugin.MediaBar.Inject." + file)!;
            string fileContents = new StreamReader(fileStream).ReadToEnd();
        
            string contentType = "text/plain";

            if (Path.GetExtension(file) == ".js")
            {
                contentType = "text/javascript";
            }
            else if (Path.GetExtension(file) == ".css")
            {
                contentType = "text/css";
            }
            else if (Path.GetExtension(file) == ".html")
            {
                contentType = "text/html";
            }
        
            return Content(fileContents, contentType);
        }

        [HttpPost("Patch/IndexHtml")]
        public ActionResult PatchIndexHtml([FromBody] PatchRequestPayload payload)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(MediaBarPlugin).Namespace}.Inject.index.html")!;
            using TextReader reader = new StreamReader(stream);

            string regex = Regex.Replace(payload.Contents!, "(</body>)", $"{reader.ReadToEnd()}$1");
        
            return Content(regex, "text/html");
        }

        [HttpPost("Patch/HomeHtmlChunk")]
        public ActionResult PatchHomeHtmlChunk([FromBody] PatchRequestPayload payload)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(MediaBarPlugin).Namespace}.Inject.home-html.chunk.js")!;
            using TextReader reader = new StreamReader(stream);

            string regex = Regex.Replace(payload.Contents!, "(id=\"homeTab\" data-index=\"0\">)", $"$1{reader.ReadToEnd()}");
        
            return Content(regex, "text/html");
        }
    }
}