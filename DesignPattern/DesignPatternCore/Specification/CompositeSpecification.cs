namespace DesignPatternCore.Specification {
    public abstract class CompositeSpecification<T> : ISpecification<T> {
        /// <summary>
        /// 是否符合规格
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public abstract bool IsSatisfiedBy(T o);
        public ISpecification<T> And(ISpecification<T> specification) {
            return new AndSpecification<T>(this, specification);
        }

        public ISpecification<T> Not(ISpecification<T> specification) {
            return new NotSpecification<T>(specification);
        }

        public ISpecification<T> Or(ISpecification<T> specification) {
            return new OrSpecification<T>(this, specification);
        }
    }
}