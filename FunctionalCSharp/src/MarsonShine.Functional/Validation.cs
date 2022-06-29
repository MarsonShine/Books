namespace MarsonShine.Functional
{
    using static F;
    public static partial class F
    {
        public static Validation<T> Valid<T>(T value) => new(value);

        // create a Validation in the Invalid state
        public static Validation.Invalid Invalid(params Error[] errors) => new(errors);
        public static Validation<R> Invalid<R>(params Error[] errors) => new Validation.Invalid(errors);
        public static Validation.Invalid Invalid(IEnumerable<Error> errors) => new(errors);
        public static Validation<R> Invalid<R>(IEnumerable<Error> errors) => new Validation.Invalid(errors);
    }
    public struct Validation<T>
    {
        internal IEnumerable<Error> Errors { get; }
        internal T? Value { get; }

        public bool IsValid { get; }

        public static Func<T, Validation<T>> Return = t => Valid(t);

        public static Validation<T> Fail(IEnumerable<Error> errors)
           => new(errors);

        public static Validation<T> Fail(params Error[] errors)
           => new(errors.AsEnumerable());

        private Validation(IEnumerable<Error> errors)
        {
            IsValid = false;
            Errors = errors;
            Value = default;
        }

        internal Validation(T right)
        {
            IsValid = true;
            Value = right;
            Errors = Enumerable.Empty<Error>();
        }
        public static implicit operator Validation<T>(Error error)
         => new(new[] { error });
        public static implicit operator Validation<T>(Validation.Invalid left)
           => new(left.Errors);
        public static implicit operator Validation<T>(T right) => Valid(right);

    }

    public static class Validation
    {
        public struct Invalid
        {
            internal IEnumerable<Error> Errors;
            public Invalid(IEnumerable<Error> errors) { Errors = errors; }
        }
    }
}
