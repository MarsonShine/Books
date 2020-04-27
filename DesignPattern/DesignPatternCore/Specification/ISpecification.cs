namespace DesignPatternCore.Specification {
    /// <summary>
    /// 规格接口，表示最细力度的规则
    /// </summary>
    public interface ISpecification<T> {
        /// <summary>
        /// 规格的自描述（是否符合规格定义）
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        bool IsSatisfiedBy(T o);
        ISpecification<T> And(ISpecification<T> specification);
        ISpecification<T> Or(ISpecification<T> specification);
        ISpecification<T> Not(ISpecification<T> specification);
    }
}