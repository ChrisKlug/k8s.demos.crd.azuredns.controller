using k8s.demos.crd.azuredns.controller.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace k8s.demos.crd.azuredns.controller.Controllers
{
    public abstract class KubernetesResourceController<T> : IHostedService
    {
        private readonly ILeaderSelector _leaderSelector;
        private readonly ILogger<KubernetesResourceController<T>> _logger;
        private Timer _leaderCheckTimer;
        private Watcher<T> _watcher;

        public KubernetesResourceController(IKubernetes kubernetes, ILeaderSelector leaderSelector, ILogger<KubernetesResourceController<T>> logger)
        {
            Kubernetes = kubernetes;
            _leaderSelector = leaderSelector;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _leaderCheckTimer = new Timer(async x =>
            {
                var isLeader = await _leaderSelector.IsLeader();
                if (isLeader)
                {
                    _leaderCheckTimer.Dispose();
                    _leaderCheckTimer = null;
                    OnPromotedToLeader();
                }

            }, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_leaderCheckTimer != null)
                _leaderCheckTimer.Dispose();

            if (_watcher != null)
                _watcher.Dispose();

            return Task.CompletedTask;
        }

        private void OnPromotedToLeader()
        {
            _logger.LogInformation("Instance promoted to leader! Starting monitoring...");

            var response = Kubernetes.ListClusterCustomObjectWithHttpMessagesAsync(Group, Version, Plural, watch: true);
            _watcher = response.Watch((Action<WatchEventType, T>)(async (type, item) => await OnResourceChange(type, item)));
        }

        protected abstract Task OnResourceChange(WatchEventType type, T item);

        protected IKubernetes Kubernetes { get; private set; }

        protected abstract string Group { get; }
        protected abstract string Version { get; }
        protected abstract string Plural { get; }
    }
}
