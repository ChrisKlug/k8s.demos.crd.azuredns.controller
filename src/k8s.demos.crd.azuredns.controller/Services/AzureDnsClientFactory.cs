using System.Collections.Concurrent;
using static k8s.demos.crd.azuredns.controller.Models.AzureDnsCredential;

namespace k8s.demos.crd.azuredns.controller.Services
{
    public class AzureDnsClientFactory : IDnsClientFactory
    {
        private readonly ConcurrentDictionary<string, AzureDnsCredentialSpec> _credentials;

        public AzureDnsClientFactory(ConcurrentDictionary<string, AzureDnsCredentialSpec> credentials)
        {
            _credentials = credentials;
        }

        public bool TryGetClient(string dnsZone, out IDnsClient client)
        {
            client = default;

            if (!_credentials.TryGetValue(dnsZone, out var creds))
            {
                return false;
            }

            client = new AzureDnsClient(creds);

            return true;
        }
    }
}
