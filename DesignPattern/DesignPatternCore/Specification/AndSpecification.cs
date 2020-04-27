namespace DesignPatternCore.Specification {
    public class AndSpecification<T> : CompositeSpecification<T> {
        private readonly ISpecification<T> _left;
        private readonly ISpecification<T> _right;

        public AndSpecification(ISpecification<T> left, ISpecification<T> right) {
            _left = left;
            _right = right;
        }

        public override bool IsSatisfiedBy(T o) {
            return this._left.IsSatisfiedBy(o) &&
                this._right.IsSatisfiedBy(o);
        }
    }
}