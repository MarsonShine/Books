using static MarsonShine.Functional.F;
using NUnit.Framework;
using Unit = System.ValueTuple;
using MarsonShine.Functional;

namespace Demo.Examples._7
{
    public delegate Validation<T> Validator<T>(T t);
    // public static Validator<BookTransfer> DateNotPast()
    public static class ValidationStrategies
    {
        // public static Validator<T> FailFast<T>(IEnumerable<Validator<T>> validators) => t => {
        //     var errors = validators.Map(validate => validate(t))
        //         .Bind(v => v)
        // }
    }
}