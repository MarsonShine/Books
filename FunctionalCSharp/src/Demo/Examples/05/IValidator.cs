namespace Demo.Examples._5
{
    public interface IValidator<T>
    {
        bool IsValid(T value);
    }
}
