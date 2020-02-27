namespace CSharpGuide.random {
    public interface IDistribution<T> {
        T Sample();
    }
}