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

        public async Task AddRecordset(string name, string type, int ttlSeconds, string[] ipAddresses)
        {
            if (!type.Equals("A", System.StringComparison.OrdinalIgnoreCase))
            {
                throw new System.Exception("Only supports adding A records at the moment");
            }

            var dnsClient = await GetClient();

            var recordSetParams = new RecordSet();
            recordSetParams.TTL = ttlSeconds;
            recordSetParams.ARecords = new List<ARecord>(ipAddresses.Select(x => new ARecord(x)));

            var recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(_credentials.ResourceGroup, _credentials.DnsZone, name, RecordType.A, recordSetParams);
        }

        public async Task RemoveRecordset(string name, string type)
        {
            if (!type.Equals("A", System.StringComparison.OrdinalIgnoreCase))
            {
                throw new System.Exception("Only supports adding A records at the moment");
            }

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
