using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.MediaBar.Controllers;

[Route("[controller]")]
public class MediaBarController : ControllerBase
{
    [HttpGet("{file}")]
    public ActionResult GetFile([FromRoute] string file)
    {
        Stream fileStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Jellyfin.Plugin.MediaBar.Inject." + file);
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
}