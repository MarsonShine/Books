using System.Threading.Tasks;

namespace AsyncPatternInNet {
    public class BaseClass {
        public virtual async Task<int> MethodAsync() {
            return await Task.FromResult(default(int));
        }
    }

    class SubClass : BaseClass {
        public override Task<int> MethodAsync() {
            return new Task<int>(() => 1);
        }
    }
}