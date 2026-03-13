<Query Kind="Program">
  <Namespace>Substitution = System.Collections.Generic.Dictionary&lt;UserQuery.Hole, UserQuery.BinTerm&gt;</Namespace>
</Query>

#nullable enable

void Main()
{
	//Hole X = new("X"), Y = new("Y");
	//Constant two = new(2), emptyStack = new("EmptyStack");
	//var stackOne = ImStack(X, emptyStack);
	//var term1 = ImStack(X, stackOne);
	//var term2 = ImStack(two, Y);
	//term1.Dump();
	//term2.Dump();

	//Hole W = new("W"), Z = new("Z");
	//var term3 = Blah(W, Blah(X, Blah(Y, Blah(Z, two))));
	//var term4 = Blah(Blah(Blah(Blah(two, Z), Y), X), W);
	//var subst = term3.Unify(term3);
	//foreach (var hole in subst!.Keys)
	//	$"{hole.Name} -> {subst[hole]}".Dump();
	//term3.Substitute(subst).Dump();

	var list = new Constant("list");
	var dble = new Constant("double");
	var func = new Constant("Func");

	Symbol Frob(BinTerm ret, BinTerm sig) => new Symbol("Frob", ret, sig);
	Symbol Generic(BinTerm name, BinTerm args) =>
		new Symbol("Generic", name, args);
	Symbol Signature(BinTerm generics, BinTerm parameters) =>
		new Symbol("Signature", generics, parameters);
	Symbol Comma(BinTerm c1, BinTerm c2) => new Symbol("Comma", c1, c2);

	Hole A = new("A"), B = new("B"), T = new("T"), U = new("U"),
		 V = new("V"), X = new("X");

	var term1 = Frob(Generic(list, B), Signature(Comma(A, B),
		Comma(A, Generic(func, Comma(A, B)))));

	var term2 = Frob(V, Signature(Comma(T, U),
		Comma(dble, Generic(func, Comma(X, Generic(list, X))))));

	var subst = term1.Unify(term2);

	Console.WriteLine($"T -> {T.Substitute(subst)}");
	Console.WriteLine($"U -> {U.Substitute(subst)}");
	Console.WriteLine($"V -> {V.Substitute(subst)}");
	Console.WriteLine($"X -> {X.Substitute(subst)}");
}

Symbol ImStack(BinTerm left, BinTerm right) => new("ImStack", left, right);

Symbol Blah(BinTerm left, BinTerm right) => new("Blah", left, right);

// You can define other methods, fields, classes and namespaces here
// 定义
// 置换（Substitution）是一个可变字典，健是占位符，值是项（item）。它回答了“这个占位符里放什么项？”的问题
// 如果一个占位符是置换中的健，则它是绑定的；如果不是，则它是自由的。（ContainsKey)
// 一阶二元项统一问题是：给定两个二元项，找到一个置换，使得当所有绑定的占位符被替换为对应的项后，这两个项相等。
public abstract record class BinTerm
{
	public bool IsBoundHole(Substitution subst) => this is Hole h && subst.ContainsKey(h);

	public BinTerm Substitute(Substitution subst)
	{
		if (this.IsBoundHole(subst))
			return subst[(Hole)this].Substitute(subst);
		if (this is Symbol s)
		{
			var left = s.Left.Substitute(subst);
			var right = s.Right.Substitute(subst);
			if (!ReferenceEquals(left, s.Left) ||
				!ReferenceEquals(right, s.Right))
				return new Symbol(s.Name, left, right);
		}
		return this;
	}

	public IEnumerable<Hole> AllHoles()
	{
		var stack = new Stack<BinTerm>();
		stack.Push(this);
		var seen = new HashSet<Hole>();
		while (stack.Count > 0)
		{
			var current = stack.Pop();
			if (current is Hole h && !seen.Contains(h))
			{
				seen.Add(h);
				yield return h;
			}
			else if (current is Symbol s)
			{
				stack.Push(s.Right); // 因为是栈结构，先进后出，所以先处理右节点
				stack.Push(s.Left);
			}
		}
	}

	// Robinson 出现检查算法
	public BinTerm LookUpBoundHole(Substitution subst)
	{
		var newTerm = this;
		while (newTerm.IsBoundHole(subst))
		{
			newTerm = subst[(Hole)newTerm];
		}
		return newTerm;
	}

	public Substitution? Unify(BinTerm t2) // #B 返回置换，如果失败则返回 null
	{
		var subst = new Substitution();
		return Unify(this, t2, subst) ? subst : null;
	}

	public static bool Unify(BinTerm t1, BinTerm t2, Substitution subst) // #C 失败时返回 false
	{
		t1 = t1.LookUpBoundHole(subst);
		t2 = t2.LookUpBoundHole(subst);
		if (t1 == t2)
			return true;

		if (t1 is Hole h1 && h1.OccursIn(t2, subst)) // #D 到这里 t1 和 t2 都不是绑定的占位符
		{
			subst.Add(h1, t2);
			return true;
		}
		if (t2 is Hole h2 && h2.OccursIn(t1, subst))
		{
			subst.Add(h2, t1);
			return true;
		}
		if (t1 is Symbol s1 && t2 is Symbol s2 && s1.Name == s2.Name)
			return Unify(s1.Left, s2.Left, subst) &&
			Unify(s1.Right, s2.Right, subst);

		return false;
	}
}

public sealed record class Constant(object Value) : BinTerm
{
	public override string ToString() => $"{Value}";
}

public sealed record class Hole(string Name) : BinTerm
{
	public override string ToString() => Name;

	public bool OccursIn(BinTerm term, Substitution subst)
	{
		foreach (Hole hole in term.AllHoles())
		{
			if (this == hole) // #D “this”是否直接用在项中？
				return true;
			if (hole.IsBoundHole(subst) && this.OccursIn(subst[hole], subst)) // #E “this”是否用在了稍后将被替换到项中的任何东西里？
				return true;
		}
		return false;
	}
}

public sealed record class Symbol(string Name, BinTerm Left, BinTerm Right) : BinTerm
{
	public override string ToString() => $"{Name}({Left},{Right})";
}
