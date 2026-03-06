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

	// 数独
	var sudoku = ImGraph<int>.Empty;
	for (int i = 0; i < 9; i += 1)
		sudoku = sudoku.AddClique([.. Enumerable.Range(i * 9, 9)]);
for (int i = 0; i < 9; i += 1)
		sudoku = sudoku.AddClique([.. Enumerable.Range(0, 9).Select(x => x * 9 + i)]);
sudoku = sudoku
	.AddClique([00, 01, 02, 09, 10, 11, 18, 19, 20])
    .AddClique([03, 04, 05, 12, 13, 14, 21, 22, 23])
	.AddClique([06, 07, 08, 15, 16, 17, 24, 25, 26])
	.AddClique([27, 28, 29, 36, 37, 38, 45, 46, 47])
	.AddClique([30, 31, 32, 39, 40, 41, 48, 49, 50])
	.AddClique([33, 34, 35, 42, 43, 44, 51, 52, 53])
	.AddClique([54, 55, 56, 63, 64, 65, 72, 73, 74])
	.AddClique([57, 58, 59, 66, 67, 68, 75, 76, 77])
	.AddClique([60, 61, 62, 69, 70, 71, 78, 79, 80]);

	var digits = ImmutableHashSet<char>.Empty
		.Add('1').Add('2').Add('3').Add('4').Add('5')
		.Add('6').Add('7').Add('8').Add('9');
		
	string puzzle =
	"  8 274  " +
	"         " +
	" 6 38  52" +
	"      32 " +
	"1   7   4" +
	" 92      " +
	"78  62 1 " +
	"         " +
	"  384 5  ";
	var initial = ImMulti<int, char>.Empty;
	foreach (var cell in sudoku.Nodes)
		initial = initial.SetItem(cell, digits);
	for (int i = 0; i < puzzle.Length; i += 1)
		if (puzzle[i] != ' ')
			initial = initial.SetSingle(i, puzzle[i]);
	var reducer = new GraphColorReducer<int, char>(sudoku);
	var backtracker = new Backtracker<int, char>(reducer);
	var solution = backtracker.Solve(initial).First();
	for (int i = 0; i < 81; i += 1)
	{
		Console.Write(solution[i].Single());
		if (i % 9 == 8)
			Console.WriteLine();
	}
}

public struct Color
{
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

interface IReducer<K, V>
where K : notnull
where V : notnull
{
	ImMulti<K, V> Reduce(ImMulti<K, V> solver);
}

sealed class GraphColorReducer<N, C> : IReducer<N, C>
where N : notnull
where C : notnull
{
	private readonly ImGraph<N> graph;
	public GraphColorReducer(ImGraph<N> graph) => this.graph = graph;
	private (ImMulti<N, C>, bool) ReduceOnce(ImMulti<N, C> attempt)
	{
		bool progress = false;
		var result = attempt;
		foreach (N n1 in attempt.Keys.Where(k => attempt[k].Count == 1)) //#A
		{
			C c = attempt[n1].Single(); //#B
			var elim = graph.Edges(n1).Where(n => attempt.HasValue(n,c));
			foreach (N n2 in elim)
			{
				result = result.Remove(n2, c); //#C
				progress = true;
			}
		}
		return (result, progress);
	}

	public ImMulti<N, C> Reduce(ImMulti<N, C> attempt)
	{
		bool progress = false;
		do
		{
			(attempt, progress) = ReduceOnce(attempt); //#D	
		} while (progress);
		return attempt;
	}
}

sealed class Backtracker<N, C> where N : notnull where C : notnull
{
	private readonly IReducer<N, C> reducer;
	public Backtracker(IReducer<N, C> reducer) => this.reducer = reducer;
	public IEnumerable<ImMulti<N, C>> Solve(ImMulti<N, C> attempt)
	{
		attempt = reducer.Reduce(attempt);
		if (attempt.Keys.Any(k => attempt[k].IsEmpty))
			return [];
		if (attempt.Keys.All(k => attempt[k].Count == 1))
			return [attempt];
		N guessKey = attempt.Keys.Where(k => attempt[k].Count > 1).First();

		return attempt[guessKey]
					.SelectMany(v =>
		Solve(attempt.SetSingle(guessKey, v)));
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