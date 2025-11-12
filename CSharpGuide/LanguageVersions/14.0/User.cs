namespace Program;
public class User
{
    public string FirstName
    {
        get => field;
        set => field = value?.Trim()
            ?? throw new ArgumentNullException(nameof(value));
    }

    public int Age
    {
        get => field;
        set => field = value < 0 ? throw new ArgumentOutOfRangeException(nameof(value)) : value;
    }
}
