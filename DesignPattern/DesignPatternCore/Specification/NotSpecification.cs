namespace DesignPatternCore.Specification {
    public class NotSpecification<T> : CompositeSpecification<T> {
        private ISpecification<T> _specification;

        public NotSpecification(ISpecification<T> specification) {
            this._specification = specification;
        }

        public override bool IsSatisfiedBy(T o) {
            return !_specification.IsSatisfiedBy(o);
        }
    }
}