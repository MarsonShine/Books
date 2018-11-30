using System.Linq;
using System.Threading.Tasks;

namespace _10OOP {
    public static class AsyncInitialization {
        public static Task WhenAllInitializedAsync(params object[] objects) {
            return Task.WhenAll(objects.OfType<IAsyncInitialize>()
                .Select(p => p.Initialization));
        }
    }
}