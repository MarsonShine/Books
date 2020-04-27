namespace DesignPatternCore.Specification {
    public class OrSpecification<T> : CompositeSpecification<T> {
        private ISpecification<T> _left;
        private ISpecification<T> _right;

        public OrSpecification(CompositeSpecification<T> left, ISpecification<T> right) {
            _left = left;
            _right = right;
        }

        public override bool IsSatisfiedBy(T o) => _left.IsSatisfiedBy(o) || _right.IsSatisfiedBy(o);
    }
}