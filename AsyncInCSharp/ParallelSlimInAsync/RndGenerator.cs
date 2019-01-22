using System;
using System.Threading.Tasks;

namespace ParallelSlimInAsync {
    public class RndGenerator : IRndGenerator {
        private readonly Random _random;
        private const int RANDOM_SEED = 1000;
        public RndGenerator() {
            _random = new Random(RANDOM_SEED);
        }
        public async Task<int> GetNextNumber() {
            //生成一个随机数...
            await Task.Delay(TimeSpan.FromSeconds(3));
            return _random.Next();
        }
    }
}