using k8s.Models;
using System;

namespace k8s.demos.crd.azuredns.controller.Models
{
    public class DnsRecordset : KubernetesObject
    {
        public const string Group = "demos.fearofoblivion.com";
        public const string Version = "v1";
        public const string Plural = "dnsrecordsets";
        public const string StatusAnnotationName = Group + "/status";

        public V1ObjectMeta Metadata { get; set; }
        public DnsRecordsetSpec Spec { get; set; }
        public Statuses Status => Metadata.Annotations.ContainsKey(StatusAnnotationName) ? Enum.Parse<Statuses>(Metadata.Annotations[StatusAnnotationName]) : Statuses.Unknown;

        public class DnsRecordsetSpec
        {
            public string DnsZone { get; set; }
            public DnsRecord[] RecordSets { get; set; }
        }

        public class DnsRecord
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public int TtlSecs { get; set; }
            public string[] IpAddresses { get; set; }
        }

        public enum Statuses
        {
            Unknown,
            Retrying,
            Creating,
            Removing,
            RetryingRemove,
            Done
        }
    }
}
