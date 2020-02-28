using System.Collections.Generic;

/// <summary>
/// 概率质量函数，probability mass function，是离散随机变量在各特定取值上的概率
/// 与概率密度函数不同 probability density function，概率密度函数是连续随机变量定义的
/// </summary>
namespace CSharpGuide.random {
    public interface IDiscreteDistribution<T> : IDistribution<T> {
        IEnumerable<T> Support();
        int Weight(T t);
    }
}