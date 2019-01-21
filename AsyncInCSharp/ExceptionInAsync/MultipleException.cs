using System.Threading.Tasks;

namespace ExceptionInAsync {
    public class MultipleException {
        public void HandleException() {
            var task = new Task(() => 2);
            Task<int[]> allTask = Task.WhenAll(task);
            try {
                await allTask;
            } catch {
                foreach (Exception ex in allTask.Exception.InnerExceptions) {
                    //Do something with exception
                }
            }
        }
    }
}