using k8s.Models;

namespace k8s.demos.crd.azuredns.controller.Models
{
    public class AzureDnsCredential : KubernetesObject
    {
        public const string Group = "demos.fearofoblivion.com";
        public const string Version = "v1";
        public const string Plural = "azurednscredentials";

        public V1ObjectMeta Metadata { get; set; }
        public AzureDnsCredentialSpec Spec { get; set; }

        public class AzureDnsCredentialSpec
        {
            public string SubscriptionId { get; set; }
            public string TenantId { get; set; }
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
            public string DnsZone { get; set; }
            public string ResourceGroup { get; set; }
        }
    }
}
