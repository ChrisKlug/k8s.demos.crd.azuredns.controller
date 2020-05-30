using System.Threading.Tasks;

namespace k8s.demos.crd.azuredns.controller.Services
{
    public interface ILeaderSelector
    {
        Task<bool> IsLeader();
    }
}
