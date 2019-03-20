using System;
using System.Reactive.Linq;

namespace _06UnitTest {
    public class MyTimeoutClass {
        private readonly IHttpService _httpService;
        public MyTimeoutClass(IHttpService httpService) {
            _httpService = httpService;
        }

        public IObservable<string> GetStringWithTimeout(string url) {
            return _httpService.GetString(url)
                .Timeout(TimeSpan.FromSeconds(1));
        }
    }
}