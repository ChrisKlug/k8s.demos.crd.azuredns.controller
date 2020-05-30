using System.Threading.Tasks;

namespace k8s.demos.crd.azuredns.controller.Services
{

    public interface IDnsClient
    {
        Task AddRecordset(string name, string type, int ttlSeconds, string[] ipAddresses);
        Task RemoveRecordset(string name, string type);
    }
}
