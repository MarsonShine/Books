using System.Collections;

namespace MySourceGenerator
{
    // https://github.com/andrewlock/blog-examples/blob/master/NetEscapades.EnumGenerators/src/NetEscapades.EnumGenerators/EquatableArray.cs
    public readonly struct EquatableArray<T>(T[] _array) : IEquatable<EquatableArray<T>>, IEnumerable<T>
        where T : IEquatable<T>
    {
        public int Count => _array.Length;

        public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
        {
            return !left.Equals(right);
        }

        public bool Equals(EquatableArray<T> other)
        {
            return AsSpan().SequenceEqual(other.AsSpan());
        }

        public override bool Equals(object obj)
        {
            return obj is EquatableArray<T> array && Equals(this, array);
        }

        public override int GetHashCode()
        {
            if (_array is not T[] array)
            {
                return 0;
            }
            HashCode hashCode = default;

            foreach (T item in array)
            {
                hashCode.Add(item);
            }

            return hashCode.ToHashCode();
        }

        public ReadOnlySpan<T> AsSpan()
        {
            return _array.AsSpan();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>)(_array ?? [])).GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)(_array ?? [])).GetEnumerator();
        }
    }
}
