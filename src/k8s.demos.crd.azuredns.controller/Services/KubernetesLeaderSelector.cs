using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace k8s.demos.crd.azuredns.controller.Services
{
    public class KubernetesLeaderSelector : ILeaderSelector
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<KubernetesLeaderSelector> _logger;
        private readonly string _endpoint;

        public KubernetesLeaderSelector(IHttpClientFactory httpClientFactory, ILogger<KubernetesLeaderSelector> logger)
            : this(httpClientFactory, logger, "http://localhost:4040")
        {
        }
        public KubernetesLeaderSelector(IHttpClientFactory httpClientFactory, ILogger<KubernetesLeaderSelector> logger, string endpoint)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _endpoint = endpoint;
        }

        public async Task<bool> IsLeader()
        {
            _logger.LogInformation("Getting leader information from " + _endpoint);
            var client = _httpClientFactory.CreateClient();
            string response;
            try
            {
                response = await client.GetStringAsync(_endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not reach leader selection endpoint");
                return false;
            }
            _logger.LogInformation("Got leader information " + response);

            return response.Contains(Environment.MachineName);
        }
    }
}
