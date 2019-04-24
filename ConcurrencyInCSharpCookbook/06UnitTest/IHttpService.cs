using System;

namespace _06UnitTest {
    public interface IHttpService {
        IObservable<string> GetString(string url);
    }
}