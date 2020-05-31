using System.Threading.Tasks;

namespace k8s.demos.crd.azuredns.controller.Services
{

    public interface IDnsClient
    {
        Task AddARecordset(string name, int ttlSeconds, string[] ipAddresses);
        Task AddTxtRecordset(string name, int ttlSeconds, string[] values);
        Task AddCnameRecordset(string name, int ttlSeconds, string value);
        Task RemoveARecordset(string name);
        Task RemoveTxtRecordset(string name);
        Task RemoveCNameRecordset(string name);
    }
}
