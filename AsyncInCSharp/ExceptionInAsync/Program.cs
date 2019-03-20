using System;
using System.Threading.Tasks;

namespace ExceptionInAsync {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello World!");
        }

        public void SyncThrowException() {

        }

        private Task<int> GetIntegerAsync(int number) {
            if (number == 0) throw new ArgumentException(nameof(number));
            return GetIntegerInternalAsync(number);
        }

        private Task<int> GetIntegerInternalAsync(int number) {
            return Task.FromResult(number);
        }

        private async void AlexsMethod() {
            try {
                await DelayForever();
            } finally {
                //
            }
        }

        private Task DelayForever() {
            return new TaskCompletionSource<object>().Task;
        }
    }
}