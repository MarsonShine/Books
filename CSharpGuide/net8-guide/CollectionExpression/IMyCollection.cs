using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace net8_guide.CollectionExpression
{
	/*
	 * CollectionBuilder 特性也可以用在接口上
	 */
	[CollectionBuilder(typeof(MyImplementCollectionBuilder), nameof(MyImplementCollectionBuilder.Create))]
	public interface IMyCollection<T> : IEnumerable<T>
	{
	}
}
