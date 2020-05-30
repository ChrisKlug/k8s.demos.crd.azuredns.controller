namespace k8s.demos.crd.azuredns.controller.Services
{
    public interface IDnsClientFactory
    {
        bool TryGetClient(string dnsZone, out IDnsClient client);
    }
}
