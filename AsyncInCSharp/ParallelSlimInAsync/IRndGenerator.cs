using System.Threading.Tasks;

namespace ParallelSlimInAsync {
    public interface IRndGenerator : IActor {
        Task<int> GetNextNumber();
    }
}