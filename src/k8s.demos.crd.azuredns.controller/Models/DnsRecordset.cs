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
            public ARecord[] ARecords { get; set; }
            public TxtRecord[] TxtRecords { get; set; }
            public CNameRecord[] CNameRecords { get; set; }
        }

        public abstract class DnsRecord
        {
            public string Name { get; set; }
            public int TtlSecs { get; set; }
        }

        public class ARecord : DnsRecord
        {
            public string[] IpAddresses { get; set; }
        }

        public class TxtRecord : DnsRecord
        {
            public string[] Values { get; set; }
        }

        public class CNameRecord : DnsRecord
        {
            public string Value { get; set; }
        }

        public enum Statuses
        {
            Unknown,
            Retrying,
            Creating,
            Removing,
            Done
        }
    }
}
