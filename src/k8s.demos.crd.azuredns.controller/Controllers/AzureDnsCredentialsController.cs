using k8s.demos.crd.azuredns.controller.Models;
using k8s.demos.crd.azuredns.controller.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using static k8s.demos.crd.azuredns.controller.Models.AzureDnsCredential;

namespace k8s.demos.crd.azuredns.controller.Controllers
{
    public class AzureDnsCredentialsController : KubernetesResourceController<AzureDnsCredential>
    {
        private readonly ILogger<AzureDnsCredentialsController> _logger;
        private readonly ConcurrentDictionary<string, AzureDnsCredentialSpec> _credentials;

        public AzureDnsCredentialsController(IKubernetes kubernetes, ILeaderSelector leaderSelector, ILoggerFactory loggerFactory, ConcurrentDictionary<string, AzureDnsCredentialSpec> credentials)
            : base(kubernetes, leaderSelector, loggerFactory.CreateLogger<KubernetesResourceController<AzureDnsCredential>>())
        {
            _logger = loggerFactory.CreateLogger<AzureDnsCredentialsController>();
            _credentials = credentials;
        }

        protected override Task OnResourceChange(WatchEventType type, AzureDnsCredential item)
        {
            switch (type)
            {
                case WatchEventType.Added:
                case WatchEventType.Modified:
                    _credentials[item.Spec.DnsZone] = item.Spec;
                    break;
                case WatchEventType.Deleted:
                    _credentials.Remove(item.Spec.DnsZone, out var _);
                    break;
            }

            return Task.CompletedTask;
        }

        protected override string Group => AzureDnsCredential.Group;
        protected override string Version => AzureDnsCredential.Version;
        protected override string Plural => AzureDnsCredential.Plural;
    }
}
