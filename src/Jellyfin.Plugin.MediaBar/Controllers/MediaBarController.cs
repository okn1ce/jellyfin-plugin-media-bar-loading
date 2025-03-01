using System.Reflection;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.MediaBar.Helpers;
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
            return Content(TransformationPatches.IndexHtml(payload), "text/html");
        }

        [HttpPost("Patch/HomeHtmlChunk")]
        public ActionResult PatchHomeHtmlChunk([FromBody] PatchRequestPayload payload)
        {
            return Content(TransformationPatches.HomeHtmlChunk(payload), "text/html");
        }

        [HttpPost("Patch/MainJellyfinBundle")]
        public ActionResult PatchMainBundle([FromBody] PatchRequestPayload payload)
        {
            return Content(TransformationPatches.MainBundle(payload), "text/html");
        }
    }
}