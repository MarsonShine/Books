using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace chapter_3
{
    public class Deque<T> : IDeque<T>
    {
        private interface IMini : IEnumerable<T>
        {
            public int Size { get; }
            IMini PushLeft(T item);
            IMini PushRight(T item);
            IMini PopLeft();
            IMini PopRight();
            T Left();
            T Right();
            public IEnumerable<IMini> TwosAndThrees(IMini mini);

        }

        private record One(T item) : IMini
        {
            public int Size => 1;

            public IEnumerator<T> GetEnumerator()
            {
                yield return item;
            }

            public T Left() => item;

            public IMini PopLeft() => throw new InvalidOperationException();

            public IMini PopRight() => throw new InvalidOperationException();

            public IMini PushLeft(T item) => new Two(item, this.item);

            public IMini PushRight(T item) => new Two(this.item, item);

            public T Right() => item;

            public IEnumerable<IMini> TwosAndThrees(IMini mini) => mini switch
            {
                One or Two => [mini.PushLeft(Left())],
                _ => [PushRight(mini.Left()), mini.PopLeft()]
            };

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private record Two(T item1, T item2) : IMini
        {
            public int Size => 2;

            public IEnumerator<T> GetEnumerator()
            {
                yield return item1;
                yield return item2;
            }

            public T Left() => item1;

            public IMini PopLeft() => new One(item2);

            public IMini PopRight() => new One(item1);

            public IMini PushLeft(T item) => new Three(item, item1, item2);

            public IMini PushRight(T item) => new Three(item1, item2, item);
            public T Right() => item2;

            public IEnumerable<IMini> TwosAndThrees(IMini mini) => mini switch
            {
                One => [PushRight(mini.Left())],
                Two or Three => [this, mini],
                _ => [PushRight(mini.Left()), mini.PopLeft()]
            };

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private record Three(T item1, T item2, T item3) : IMini
        {
            public int Size => 3;
            public IEnumerator<T> GetEnumerator()
            {
                yield return item1;
                yield return item2;
                yield return item3;
            }
            public T Left() => item1;
            public IMini PopLeft() => new Two(item2, item3);
            public IMini PopRight() => new Two(item1, item2);
            public IMini PushLeft(T item) => throw new InvalidOperationException();
            public IMini PushRight(T item) => throw new InvalidOperationException();
            public T Right() => item3;
            public IEnumerable<IMini> TwosAndThrees(IMini mini) => mini switch
            {
                One => [PopRight(), mini.PushLeft(Right())],
                Two or Three => [this, mini],
                _ => [this, mini.PopRight().PopRight(), mini.PopLeft().PopLeft()]
            };
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private record Four(T item1, T item2, T item3, T item4) : IMini
        {
            public int Size => 4;

            public IEnumerator<T> GetEnumerator()
            {
                yield return item1;
                yield return item2;
                yield return item3;
                yield return item4;
            }

            public T Left() => item1;

            public IMini PopLeft() => new Three(item2, item3, item4);

            public IMini PopRight() => new Three(item1, item2, item3);

            public IMini PushLeft(T item) => throw new InvalidOperationException();

            public IMini PushRight(T item) => throw new InvalidOperationException();

            public T Right() => item4;

            public IEnumerable<IMini> TwosAndThrees(IMini mini) => mini switch
            {
                One => [PopRight(), mini.PushLeft(Right())],
                Two or Three => [this, mini],
                _ => [this, mini.PopRight().PopRight(), mini.PopLeft().PopLeft()]
            };

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class EmptyDeque : IDeque<T>
        {
            public IDeque<T> Concatenate(IDeque<T> items)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<T> GetEnumerator()
            {
                yield break;
            }

            public bool IsEmpty() => true;

            public T Left() => throw new InvalidOperationException();

            public IDeque<T> PopLeft() => throw new InvalidOperationException();

            public IDeque<T> PopRight() => throw new InvalidOperationException();

            public IDeque<T> PushLeft(T item) => new SingleDeque(item);

            public IDeque<T> PushRight(T item) => new SingleDeque(item);

            public T Right() => throw new InvalidOperationException();

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public static IDeque<T> Empty { get; } = new EmptyDeque();

        private record SingleDeque(T item) : IDeque<T>
        {
            public IDeque<T> Concatenate(IDeque<T> items)
            {
                throw new NotImplementedException();
            }
            public IEnumerator<T> GetEnumerator()
            {
                yield return item;
            }
            public bool IsEmpty() => false;
            public T Left() => item;
            public IDeque<T> PopLeft() => Empty;
            public IDeque<T> PopRight() => Empty;
            public IDeque<T> PushLeft(T newItem) => new Deque<T>(new One(newItem), Deque<IMini>.Empty, new One(item));
            public IDeque<T> PushRight(T newItem) => new Deque<T>(new One(item), Deque<IMini>.Empty, new One(newItem));
            public T Right() => item;
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private readonly IMini left;
        private readonly IDeque<IMini> middle;
        private readonly IMini right;
        private Deque(IMini left, IDeque<IMini> middle, IMini right)
        {
            this.left = left;
            this.middle = middle;
            this.right = right;
        }

        public IDeque<T> Concatenate(IDeque<T> items) => 
            items is Deque<T> d ?
                new Deque<T>(
                    left,
                    middle.PushRightMany(right.TwosAndThrees(d.left)).Concatenate(d.middle),
                    d.right) :

            this.PushRightMany(items);

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in left)
                yield return item;
            foreach (var mini in middle)
                foreach (var item in mini)
                    yield return item;
            foreach (var item in right)
                yield return item;
        }

        public bool IsEmpty() => false;

        public T Left() => left.Left();

        public IDeque<T> PopLeft()
        {
            if (left.Size > 1)
                return new Deque<T>(left.PopLeft(), middle, right);
            if (!middle.IsEmpty())
                return new Deque<T>(middle.Left(), middle.PopLeft(), right);
            if (right.Size > 1)
                return new Deque<T>(new One(right.Right()), middle, right.PopLeft());
            return new SingleDeque(right.Left());
        }

        public IDeque<T> PopRight()
        {
            if (right.Size > 1)
                return new Deque<T>(left, middle, right.PopRight());
            if (!middle.IsEmpty())
                return new Deque<T>(left, middle.PopRight(), middle.Right());
            if (left.Size > 1)
                return new Deque<T>(left.PopRight(), middle, new One(left.Right()));
            return new SingleDeque(left.Right());
        }

        // 数据插入并保持平衡
        public IDeque<T> PushLeft(T value) => left.Size < 4 ?
            new Deque<T>(left.PushLeft(value), middle, right) :
            new Deque<T>(
                new Two(value, left.Left()),
                middle.PushLeft(left.PopLeft()),
                right);

        public IDeque<T> PushRight(T value) => right.Size < 4 ?
            new Deque<T>(left, middle, right.PushRight(value)) :
            new Deque<T>(
                left,
                middle.PushRight(right.PopRight()),
                new Two(right.Right(), value));

        public T Right() => right.Right();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
