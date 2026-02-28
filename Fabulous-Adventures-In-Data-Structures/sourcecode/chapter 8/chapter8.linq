<Query Kind="Program">
  <Namespace>System.Collections.Immutable</Namespace>
</Query>

void Main()
{
	var badGraph = ImMulti<string, string>.Empty
	.Add("Paraguay", "Brazil")
	.Add("Brazil", "Bolivia");
	badGraph.Dump();

	var southAmerica = ImGraph<string>.Empty
	.AddNode("Falkland Islands")
	.AddEdges("French Guiana", ["Brazil", "Suriname"])
	.AddEdges("Suriname", ["Brazil", "Guyana"])
	.AddEdges("Guyana", ["Brazil", "Venezuela"])
	.AddEdges("Venezuela", ["Brazil", "Colombia"])
	.AddEdges("Colombia", ["Brazil", "Peru", "Ecuador"])
	.AddEdges("Peru", ["Brazil", "Ecuador", "Bolivia", "Chile"])
	.AddEdges("Chile", ["Bolivia", "Argentina"])
	.AddEdges("Bolivia", ["Brazil", "Paraguay", "Argentina"])
	.AddEdges("Paraguay", ["Brazil", "Argentina"])
	.AddEdges("Argentina", ["Brazil", "Uruguay"])
	.AddEdge("Uruguay", "Brazil");
	var colors = ImmutableHashSet<Color>.Empty
		.Add(Color.Red).Add(Color.Yellow)
		.Add(Color.Green).Add(Color.Purple);
		
	southAmerica.Dump();
	colors.Dump();
	
	southAmerica.ColorGraph(colors).Dump();
}

public struct Color {
	public string value;
	public Color(string value) => this.value = value;
	
	public static Color Red = new("Red");
	public static Color Yellow = new("Yellow");
	public static Color Green = new("Green");
	public static Color Purple = new("Purple");
}

readonly struct ImMulti<K, V>
	where K : notnull
	where V : notnull
{
	private readonly ImmutableDictionary<K, ImmutableHashSet<V>> dict;
	public static ImMulti<K, V> Empty = new(ImmutableDictionary<K, ImmutableHashSet<V>>.Empty);

	private ImMulti(ImmutableDictionary<K, ImmutableHashSet<V>> dict) : this()
	{
		this.dict = dict;
	}

	public bool IsEmpty => dict.IsEmpty;
	public bool ContainsKey(K k) => dict.ContainsKey(k);
	public bool HasValue(K k, V v) => dict.ContainsKey(k) && dict[k].Contains(v);

	public IEnumerable<K> Keys => dict.Keys;
	public ImmutableHashSet<V> this[K k] => dict[k];
	public ImMulti<K, V> SetItem(K k, ImmutableHashSet<V> vs) => new(dict.SetItem(k, vs));
	public ImMulti<K, V> SetSingle(K k, V v) => SetItem(k, ImmutableHashSet<V>.Empty.Add(v));
	public ImMulti<K, V> SetEmpty(K k) => SetItem(k, ImmutableHashSet<V>.Empty);
	public ImMulti<K, V> Add(K k, V v) => ContainsKey(k) ?
		new(dict.SetItem(k, dict[k].Add(v))) :
		SetSingle(k, v);
	public ImMulti<K, V> Remove(K k) => new(dict.Remove(k));
	public ImMulti<K, V> Remove(K k, V v) => new(dict.SetItem(k, dict[k].Remove(v)));
}

public readonly struct ImGraph<N> where N : notnull
{
	public static ImGraph<N> Empty = new(ImMulti<N, N>.Empty);
	private readonly ImMulti<N, N> nodes;

	private ImGraph(ImMulti<N, N> ns)
	{
		this.nodes = ns;
	}

	public bool IsEmpty => nodes.IsEmpty;
	public bool HasNode(N n) => nodes.ContainsKey(n);
	public bool HasEdge(N n1, N n2) => nodes.HasValue(n1, n2);
	public IEnumerable<N> Nodes => nodes.Keys;
	public ImmutableHashSet<N> Edges(N n) => nodes[n];
	public ImGraph<N> AddNode(N n) => HasNode(n) ? this : new(nodes.SetItem(n, ImmutableHashSet<N>.Empty));
	public ImGraph<N> AddEdge(N n1, N n2) => new(nodes.Add(n1, n2).Add(n2, n1));
	public ImGraph<N> RemoveEdge(N n1, N n2) => new(nodes.Remove(n1, n2).Remove(n2, n1));
	public ImGraph<N> RemoveNode(N n)
	{
		var result = this;
		foreach (var n2 in Edges(n))
			result = result.RemoveEdge(n, n2);
		return new(result.nodes.Remove(n));
	}

	public ImGraph<N> AddEdges(N n1, IEnumerable<N> ns)
	{
		var result = this;
		foreach (var n2 in ns)
			result = result.AddEdge(n1, n2);
		return result;
	}
	public ImGraph<N> AddClique(IList<N> ns)
	{
		var result = this;
		for (int i = 0; i < ns.Count; i += 1)
		{
			for (int j = i + 1; j < ns.Count; j += 1)
			{
				result = result.AddEdge(ns[i], ns[j]);
			}
		}
		return result;
	}
}

public static class SimpleGraphColoring
{
	extension<N, C>(ImGraph<N> graph)
	where N : notnull
	where C : notnull
	{
		public ImmutableDictionary<N, C> ColorGraph(ImmutableHashSet<C> colors)
		{
			if (graph.IsEmpty)
				return ImmutableDictionary<N, C>.Empty;
			N last = graph.Nodes
			.Where(n => graph.Edges(n).Count < colors.Count)
			.FirstOrDefault(); //#A

			if (last is null)
				return null;
			var coloring = graph.RemoveNode(last).ColorGraph(colors);
			if (coloring is null)
				return null;
			var usedColors = graph.Edges(last).Select(n => coloring[n]); //#B
			var lastColor = colors.Except(usedColors).First(); //#C
			return coloring.Add(last, lastColor);
		}
	}
}