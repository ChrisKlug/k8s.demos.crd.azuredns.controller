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
        private readonly ILogger<DnsRecordset> _logger;
        private readonly ConcurrentDictionary<string, DnsRecordset> _recordsets = new ConcurrentDictionary<string, DnsRecordset>();
        private readonly IDnsClientFactory _dnsClientFactory;

        public DnsRecordsetController(IKubernetes kubernetes, ILeaderSelector leaderSelector, ILoggerFactory loggerFactory, IDnsClientFactory dnsClientFactory)
            : base(kubernetes, leaderSelector, loggerFactory.CreateLogger<KubernetesResourceController<DnsRecordset>>())
        {
            _logger = loggerFactory.CreateLogger<DnsRecordset>();
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
                await UpdateRecordsetStatus(recordset, DnsRecordset.Statuses.Retrying);
                new Timer(async x => await RecordsetAdded(recordset), null, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(-1));
                return;
            }

            await UpdateRecordsetStatus(recordset, DnsRecordset.Statuses.Creating);

            foreach (var record in recordset.Spec.ARecords)
            {
                try
                {
                    await client.AddARecordset(record.Name, record.TtlSecs, record.IpAddresses);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not add A recordset {record.Name} for {recordset.Spec.DnsZone}!");
                }
            }
            foreach (var record in recordset.Spec.CNameRecords)
            {
                try
                {
                    await client.AddCnameRecordset(record.Name, record.TtlSecs, record.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not add CName recordset {record.Name} for {recordset.Spec.DnsZone}!");
                }
            }
            foreach (var record in recordset.Spec.TxtRecords)
            {
                try
                {
                    await client.AddTxtRecordset(record.Name, record.TtlSecs, record.Values);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not add Txt recordset {record.Name} for {recordset.Spec.DnsZone}!");
                }
            }

            await UpdateRecordsetStatus(recordset, DnsRecordset.Statuses.Done);
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
                new Timer(async x => await RecordsetRemoved(recordset), null, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(-1));
                return;
            }

            foreach (var record in recordset.Spec.ARecords)
            {
                try
                {
                    await client.RemoveARecordset(record.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not remove recordset {record.Name} for {recordset.Spec.DnsZone}!");
                }
            }
            foreach (var record in recordset.Spec.CNameRecords)
            {
                try
                {
                    await client.RemoveCNameRecordset(record.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not remove recordset {record.Name} for {recordset.Spec.DnsZone}!");
                }
            }
            foreach (var record in recordset.Spec.TxtRecords)
            {
                try
                {
                    await client.RemoveTxtRecordset(record.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not remove recordset {record.Name} for {recordset.Spec.DnsZone}!");
                }
            }

            _recordsets.Remove(recordset.Metadata.Name, out var _);
        }
        private async Task UpdateRecordsetStatus(DnsRecordset recordset, DnsRecordset.Statuses status)
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
