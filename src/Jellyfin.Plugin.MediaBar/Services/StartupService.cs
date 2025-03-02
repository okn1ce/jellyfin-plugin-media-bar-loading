using System.IO.Pipes;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Jellyfin.Plugin.MediaBar.Helpers;
using Jellyfin.Plugin.MediaBar.Model;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.MediaBar.Services
{
    public class StartupService : IScheduledTask
    {
        public string Name => "MediaBar Startup";

        public string Key => "Jellyfin.Plugin.MediaBar.Startup";
        
        public string Description => "Startup Service for MediaBar";
        
        public string Category => "Startup Services";
        
        private readonly IServerApplicationHost m_serverApplicationHost;
        private readonly ILogger<MediaBarPlugin> m_logger;
        private readonly NamedPipeService m_namedPipeService;
        private readonly IUserManager m_userManager;
        private readonly IPlaylistManager m_playlistManager;

        public StartupService(IServerApplicationHost serverApplicationHost, ILogger<MediaBarPlugin> logger, NamedPipeService namedPipeService,
            IUserManager userManager, IPlaylistManager playlistManager)
        {
            m_serverApplicationHost = serverApplicationHost;
            m_logger = logger;
            m_namedPipeService = namedPipeService;
            m_userManager = userManager;
            m_playlistManager = playlistManager;
        }

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            m_logger.LogInformation($"MediaBar Startup. Registering file transformations.");
            
            List<JObject> payloads = new List<JObject>();

            {
                JObject payload = new JObject();
                payload.Add("id", "0dfac9d7-d898-4944-900b-1c1837707279");
                payload.Add("fileNamePattern", "index.html");
                payload.Add("transformationEndpoint", "/MediaBar/Patch/IndexHtml");
                payload.Add("transformationPipe", "Jellyfin.Plugin.MediaBar.Patch.IndexHtml");
                RegisterPipeEndpoint("Jellyfin.Plugin.MediaBar.Patch.IndexHtml", TransformationPatches.IndexHtml);
                
                payloads.Add(payload);
            }
            {
                JObject payload = new JObject();
                payload.Add("id", "e6d32b76-d54b-4946-b73e-c5c9c50575c9");
                payload.Add("fileNamePattern", "home-html\\.[a-zA-z0-9]+\\.chunk\\.js");
                payload.Add("transformationEndpoint", "/MediaBar/Patch/HomeHtmlChunk");
                payload.Add("transformationPipe", "Jellyfin.Plugin.MediaBar.Patch.HomeHtmlChunk");
                RegisterPipeEndpoint("Jellyfin.Plugin.MediaBar.Patch.HomeHtmlChunk", TransformationPatches.HomeHtmlChunk);
                
                payloads.Add(payload);
            }
            {
                JObject payload = new JObject();
                payload.Add("id", "3d171ef1-a198-48ac-9a60-f6aa98e5fd6d");
                payload.Add("fileNamePattern", "main.jellyfin.bundle.js");
                payload.Add("transformationEndpoint", "/MediaBar/Patch/MainJellyfinBundle");
                payload.Add("transformationPipe", "Jellyfin.Plugin.MediaBar.Patch.MainBundle");
                RegisterPipeEndpoint("Jellyfin.Plugin.MediaBar.Patch.MainBundle", TransformationPatches.MainBundle);
                
                payloads.Add(payload);
            }
            {
                JObject payload = new JObject();
                payload.Add("id", "8d374d6b-3c5b-464a-a2a2-96e92fa81345");
                payload.Add("fileNamePattern", "avatars/list.txt");
                payload.Add("transformationEndpoint", "/MediaBar/Avatar/List");
                payload.Add("transformationPipe", "Jellyfin.Plugin.MediaBar.AvatarsList");
                RegisterPipeEndpoint("Jellyfin.Plugin.MediaBar.AvatarsList", innerPayload =>
                {
                    return TransformationPatches.AvatarsList(innerPayload, m_playlistManager, m_userManager) ?? "";
                });
                
                payloads.Add(payload);
            }
            
            string fileTransformationPipeName = "Jellyfin.Plugin.FileTransformation.NamedPipe";
            MethodInfo? getPipePathMethod = typeof(PipeStream).GetMethod("GetPipePath", BindingFlags.Static | BindingFlags.NonPublic);
            string? pipePath = getPipePathMethod?.Invoke(null, new object[] { ".", fileTransformationPipeName }) as string;
            string? pipeDirectory = Path.GetDirectoryName(pipePath);
            
            if (Directory.Exists(pipeDirectory) && Directory.GetFiles(pipeDirectory).Contains(pipePath))
            {
                foreach (JObject payload in payloads)
                {
                    NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", fileTransformationPipeName, PipeDirection.InOut);
                    await pipeClient.ConnectAsync();
                    byte[] payloadBytes = Encoding.UTF8.GetBytes(payload.ToString(Formatting.None));
                            
                    await pipeClient.WriteAsync(BitConverter.GetBytes((long)payloadBytes.Length));
                    await pipeClient.WriteAsync(payloadBytes, 0, payloadBytes.Length);
                    
                    pipeClient.ReadByte();
                            
                    await pipeClient.DisposeAsync();
                }
            }
            else
            {
                m_logger.LogInformation("Delaying 5 seconds to ensure file transformation is ready to receive requests.");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                
                string? publishedServerUrl = m_serverApplicationHost.GetType()
                    .GetProperty("PublishedServerUrl", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(m_serverApplicationHost) as string;
                m_logger.LogInformation($"Retrieved value for published server URL: {publishedServerUrl}");
                
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(publishedServerUrl ?? $"http://localhost:{m_serverApplicationHost.HttpPort}");
                
                m_logger.LogInformation($"Setting base address to: {client.BaseAddress}.");
                m_logger.LogInformation($"Retrieving media bar payloads.");
                foreach (JObject payload in payloads)
                {
                    try
                    {
                        m_logger.LogInformation($"Registering transformation '{payload.Value<string>("id")}' with endpoint '{payload.Value<string>("transformationEndpoint")}'");
                        
                        await client.PostAsync("/FileTransformation/RegisterTransformation",
                            new StringContent(payload.ToString(Formatting.None),
                                MediaTypeHeaderValue.Parse("application/json")));
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, $"Caught exception when attempting to register file transformation. Ensure you have `File Transformation` plugin installed on your server.");
                        return;
                    }
                }
            }
        }

        private void RegisterPipeEndpoint(string pipeName, Func<PatchRequestPayload, string> handler)
        {
            m_namedPipeService.CreateNamedPipeHandler(pipeName, async stream =>
            {
                byte[] lengthBuffer = new byte[8];
                await stream.ReadExactlyAsync(lengthBuffer, 0, lengthBuffer.Length);
                long length = BitConverter.ToInt64(lengthBuffer, 0);
                        
                MemoryStream memoryStream = new MemoryStream();
                while (length > 0)
                {
                    byte[] buffer = new byte[Math.Min(length, 1024)];
                    int readBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                    length -= readBytes;
                            
                    memoryStream.Write(buffer, 0, readBytes);
                }
                        
                string rawJson = Encoding.UTF8.GetString(memoryStream.ToArray());
                
                string response = handler(JsonConvert.DeserializeObject<PatchRequestPayload>(rawJson)!);
                byte[] responseBuffer = Encoding.UTF8.GetBytes(response);
                byte[] responseLengthBuffer = BitConverter.GetBytes((long)responseBuffer.Length);
                        
                await stream.WriteAsync(responseLengthBuffer, 0, responseLengthBuffer.Length);
                await stream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
            });
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo()
            {
                Type = TaskTriggerInfo.TriggerStartup
            };
        }
    }
}