using k8s.demos.crd.azuredns.controller.Models;
using k8s.demos.crd.azuredns.controller.Services;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace k8s.demos.crd.azuredns.controller.Controllers
{
    public class DnsRecordsetController : KubernetesResourceController<DnsRecordset>
    {
        private readonly ILogger<AzureDnsCredentialsController> _logger;
        private readonly ConcurrentDictionary<string, DnsRecordset> _recordsets = new ConcurrentDictionary<string, DnsRecordset>();
        private readonly IDnsClientFactory _dnsClientFactory;

        public DnsRecordsetController(IKubernetes kubernetes, ILeaderSelector leaderSelector, ILoggerFactory loggerFactory, IDnsClientFactory dnsClientFactory)
            : base(kubernetes, leaderSelector, loggerFactory.CreateLogger<KubernetesResourceController<DnsRecordset>>())
        {
            _logger = loggerFactory.CreateLogger<AzureDnsCredentialsController>();
            _dnsClientFactory = dnsClientFactory;
        }

        protected override async Task OnResourceChange(WatchEventType type, DnsRecordset item)
        {
            switch (type)
            {
                case WatchEventType.Added:
                    await RecordsetAdded(item);
                    break;
                case WatchEventType.Modified:
                    _logger.LogWarning("Modifications are not supported at the moment");
                    break;
                case WatchEventType.Deleted:
                    await RecordsetRemoved(item);
                    break;
            }
        }

        private async Task RecordsetAdded(DnsRecordset recordset)
        {
            _recordsets[recordset.Metadata.Name] = recordset;

            if (recordset.Status == DnsRecordset.Statuses.Done)
            {
                return;
            }

            if (!_dnsClientFactory.TryGetClient(recordset.Spec.DnsZone, out var client))
            {
                _logger.LogWarning($"Missing credentials for zone {recordset.Spec.DnsZone}. Retrying...");
                await UpdateStatus(recordset, DnsRecordset.Statuses.Retrying);
                new Timer(async x => await RecordsetAdded(recordset), null, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(-1));
                return;
            }

            await UpdateStatus(recordset, DnsRecordset.Statuses.Creating);

            foreach (var record in recordset.Spec.RecordSets)
            {
                try
                {
                    await client.AddRecordset(record.Name, record.Type, record.TtlSecs, record.IpAddresses);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not add recordset!");
                }
            }

            await UpdateStatus(recordset, DnsRecordset.Statuses.Done);
        }
        private async Task RecordsetRemoved(DnsRecordset recordset)
        {
            if (recordset.Status != DnsRecordset.Statuses.Done)
            {
                _logger.LogWarning($"Removing recordset that is not completely created...");
                _recordsets.Remove(recordset.Metadata.Name, out var _);
                return;
            }

            if (!_dnsClientFactory.TryGetClient(recordset.Spec.DnsZone, out var client))
            {
                _logger.LogWarning($"Missing credentials for zone {recordset.Spec.DnsZone}. Retrying...");
                await UpdateStatus(recordset, DnsRecordset.Statuses.RetryingRemove);
                new Timer(async x => await RecordsetRemoved(recordset), null, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(-1));
                return;
            }

            foreach (var record in recordset.Spec.RecordSets)
            {
                try
                {
                    await client.RemoveRecordset(record.Name, record.Type);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not remove recordset {record.Name} for {recordset.Spec.DnsZone}!");
                }
            }

            _recordsets.Remove(recordset.Metadata.Name, out var _);
        }
        private async Task UpdateStatus(DnsRecordset recordset, DnsRecordset.Statuses status)
        {
            recordset.Metadata.Annotations[DnsRecordset.StatusAnnotationName] = status.ToString();

            var patch = new JsonPatchDocument<DnsRecordset>();
            patch.Replace(x => x.Metadata.Annotations, recordset.Metadata.Annotations);
            patch.Operations.ForEach(x => x.path = x.path.ToLower());

            var response = await Kubernetes.PatchClusterCustomObjectAsync(new V1Patch(patch), Group, Version, Plural, recordset.Metadata.Name);
        }

        protected override string Group => DnsRecordset.Group;
        protected override string Version => DnsRecordset.Version;
        protected override string Plural => DnsRecordset.Plural;
    }
}
