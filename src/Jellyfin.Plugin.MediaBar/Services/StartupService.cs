using System.IO.Pipes;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
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

        public StartupService(IServerApplicationHost serverApplicationHost, ILogger<MediaBarPlugin> logger)
        {
            m_serverApplicationHost = serverApplicationHost;
            m_logger = logger;
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
                
                payloads.Add(payload);
            }
            {
                JObject payload = new JObject();
                payload.Add("id", "e6d32b76-d54b-4946-b73e-c5c9c50575c9");
                payload.Add("fileNamePattern", "home-html\\.[a-zA-z0-9]+\\.chunk\\.js");
                payload.Add("transformationEndpoint", "/MediaBar/Patch/HomeHtmlChunk");
                
                payloads.Add(payload);
            }
            {
                JObject payload = new JObject();
                payload.Add("id", "3d171ef1-a198-48ac-9a60-f6aa98e5fd6d");
                payload.Add("fileNamePattern", "main.jellyfin.bundle.js");
                payload.Add("transformationEndpoint", "/MediaBar/Patch/MainJellyfinBundle");
                
                payloads.Add(payload);
            }
            
            string fileTransformationPipeName = "Jellyfin.Plugin.FileTransformation.NamedPipe";
            bool serverSupportsPipeCommunication = Directory.GetFiles(@"\\.\pipe\").Contains($@"\\.\pipe\{fileTransformationPipeName}");

            if (serverSupportsPipeCommunication)
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

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo()
            {
                Type = TaskTriggerInfo.TriggerStartup
            };
        }
    }
}