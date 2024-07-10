using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace net8_guide.CollectionExpression
{
	public class MyCollection : IEnumerable
	{
		private readonly List<int> list = new();
		public IEnumerator GetEnumerator() => list.GetEnumerator();

		public void Add(int val) => list.Add(val);
	}

	/* 
	 * 必须要是实现IEnumerable/IEnumberable<T>和一个public的Add方法。
	 * 这个方式编译器生成的代码如下：
	 * var c = new MyCollection();
	 * c.Add(1);
	 * c.Add(2);
	 * c.Add(3);
	 * 还可以有优化之后的方式，可以通过 CollectionBuilder 来实现
	 */
	public class MyCollection<T> : IEnumerable<T>
	{
		private readonly List<T> list = new();
		public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public void Add(T val) => list.Add(val);
	}
	[CollectionBuilder(typeof(MyBetterCollection), nameof(MyBetterCollection.Create))]
	public class MyBetterCollection
	{
		public static MyBetterCollection Create(ReadOnlySpan<int> values) => new(values);
		private readonly int[] _values;
		public MyBetterCollection(ReadOnlySpan<int> values)
		{
			_values = values.ToArray();
		}

		public IEnumerator<int> GetEnumerator() => _values.AsEnumerable().GetEnumerator();
	}
	// 如果CollectionBuilder处理泛型方法，则需要把Builder单独拿出来
	public class MyCollectionBuilder
	{
		public static MyBetterCollection<T> Create<T>(ReadOnlySpan<T> values) => new(values);
	}
	[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
	public class MyBetterCollection<T>(ReadOnlySpan<T> values)
	{
		private readonly T[] _values = values.ToArray();

		public IEnumerator<T> GetEnumerator() => _values.AsEnumerable().GetEnumerator();
	}
	[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
	public class MyImplementCollection<T>(T[] values) : IMyCollection<T>
	{
		public IEnumerator<T> GetEnumerator() => values.AsEnumerable().GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
