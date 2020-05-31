using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Rest.Azure.Authentication;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static k8s.demos.crd.azuredns.controller.Models.AzureDnsCredential;

namespace k8s.demos.crd.azuredns.controller.Services
{
    public class AzureDnsClient : IDnsClient
    {
        private readonly AzureDnsCredentialSpec _credentials;

        public AzureDnsClient(AzureDnsCredentialSpec credentials)
        {
            _credentials = credentials;
        }

        public async Task AddARecordset(string name, int ttlSeconds, string[] ipAddresses)
        {
            var recordSet = new RecordSet();
            recordSet.TTL = ttlSeconds;
            recordSet.ARecords = new List<ARecord>(ipAddresses.Select(x => new ARecord(x)));

            await AddRecordset(name, RecordType.A, recordSet);
        }
        public async Task AddCnameRecordset(string name, int ttlSeconds, string value)
        {
            var recordSet = new RecordSet();
            recordSet.TTL = ttlSeconds;
            recordSet.CnameRecord = new CnameRecord(value);

            await AddRecordset(name, RecordType.CNAME, recordSet);
        }
        public async Task AddTxtRecordset(string name, int ttlSeconds, string[] values)
        {
            var recordSet = new RecordSet();
            recordSet.TTL = ttlSeconds;
            recordSet.TxtRecords = new List<TxtRecord>(values.Select(x => new TxtRecord(new[] { x })));

            await AddRecordset(name, RecordType.TXT, recordSet);
        }

        public async Task RemoveARecordset(string name)
        {
            await RemoveRecordset(name, RecordType.A);
        }
        public async Task RemoveCNameRecordset(string name)
        {
            await RemoveRecordset(name, RecordType.CNAME);
        }
        public async Task RemoveTxtRecordset(string name)
        {
            await RemoveRecordset(name, RecordType.TXT);
        }

        private async Task AddRecordset(string name, RecordType recordType, RecordSet recordSet)
        {
            var dnsClient = await GetClient();

            await dnsClient.RecordSets.CreateOrUpdateAsync(_credentials.ResourceGroup, _credentials.DnsZone, name, recordType, recordSet);
        }
        private async Task RemoveRecordset(string name, RecordType type)
        {
            var dnsClient = await GetClient();
            await dnsClient.RecordSets.DeleteAsync(_credentials.ResourceGroup, _credentials.DnsZone, name, RecordType.A);
        }
        private async Task<DnsManagementClient> GetClient()
        {
            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(_credentials.TenantId, _credentials.ClientId, _credentials.ClientSecret);
            var dnsClient = new DnsManagementClient(serviceCreds);
            dnsClient.SubscriptionId = _credentials.SubscriptionId;

            return dnsClient;
        }
    }
}
