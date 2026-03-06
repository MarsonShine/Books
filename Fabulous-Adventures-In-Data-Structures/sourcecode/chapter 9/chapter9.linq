<Query Kind="Program" />

using static UserQuery.Doc;

void Main()
{
	TextDoc foo = new("Foo"), bar = new("Bar"), xx = new("xx"),
			yy = new("yy"), zz = new("zz"), dot = new("."), plus = new("+"),
			star = new("*"), lparen = new("("), rparen = new(")");

	var mult = Group(Indent(Group(yy + Space + star) + Space + zz));
	var sum = Group(Indent(Group(xx + Space + plus) + Space + mult));
	var parens = Group(Indent(lparen + OptBreak + sum + rparen));
	var foobar = Group(foo + Indent(Group(OptBreak + dot + bar)));
	var callfb = Group(foobar + parens);

	callfb.Pretty(30).Dump();
	callfb.Pretty(20).Dump();
	callfb.Pretty(15).Dump();

	//Expr expr = new Id("foo");
	//IExprVisitor<int> visitor = new MyExprVisitor();
	//int x = expr.Accept(visitor);

	var f = new Member(new Member(new Id("Frobozz"), "Frobulator"), "Frobulate");
	var b = new Member(new Id("Blob"), "Width");
	var c = new Member(new Id("Cob"), "Width");
	var g = new Member(new Id("Glob"), "Length");
	var q = new Member(new Member(new Id("Qbert"), "Quux"), "Qix");
	var m = new Mult(new Paren(new Add(b, c)), g);
	var call = new Call(f, m, q);
	call.Pretty(80).Dump();
}

public abstract record class Doc
{
	public static readonly int IndentSpaces = 4; //#A 为简化，我们将缩进固定为四个空格
	public static readonly Doc Empty = new TextDoc("");
	public static readonly Doc Space = new OptBreakDoc(" ");
	public static readonly Doc OptBreak = new OptBreakDoc("");

	public static TextDoc Text(string text) => new TextDoc(text); // #B 这些静态工厂方法将使后续代码更简洁
	public static GroupDoc Group(Doc doc) => new GroupDoc(doc);
	public static IndentDoc Indent(Doc doc) => new IndentDoc(doc);
	public static ConcatDoc Concat(Doc left, Doc right) =>
		new ConcatDoc(left, right);
	public static ConcatDoc operator +(Doc left, Doc right) =>
		Concat(left, right); // #C 文档可以使用 + 连接，就像字符串一样

	private bool Fits(int width)
	{
		var docs = new Stack<Doc>(); // #A 将此栈设为可变的很合理
		docs.Push(this); // #B 将“this”放入工作栈
		int used = 0; // #C 目前为止已使用了多少水平空间？
		while (true)
		{
			if (used > width) // #D 是否已使用超过可用空间？那么“this”不适合
				return false;
			if (docs.Count == 0) // #E 是否没有剩余工作项？那么“this”适合
				return true;
			Doc doc = docs.Pop();
			switch (doc)
			{
				case TextDoc td:
					used += td.text.Length;
					break;
				case GroupDoc gd:
					docs.Push(gd.Doc);
					break;
				case IndentDoc id:
					docs.Push(id.Doc); // #F 组和缩进仅推入其子节点
					break;
				case OptBreakDoc bd:
					used += bd.text.Length; // #G 假设可选中断不是换行符
					break;
				case ConcatDoc cd: // 
					docs.Push(cd.Right); // #H 左子节点先处理，所以后推入
					docs.Push(cd.Left);
					break;
			}
		}
	}

	public string Pretty(int width)
	{
		var sb = new StringBuilder();
		int used = 0; // #A 这一行已使用的字符计数
		var docs = new Stack<(bool, int, Doc)>(); // #B 在工作栈中存储所需的中断行为和缩进级别
		docs.Push((true, 0, new GroupDoc(this))); // #C 创建一个外部组，并尝试在不换行、缩进零空格的情况下打印它

		while (docs.Count > 0) // #D 当没有剩余工作项时，就完成了
		{
			(bool fits, int indent, Doc doc) = docs.Pop(); // #E 将需要的三部分信息弹出到局部变量
			switch (doc)
			{
				case TextDoc td:
					sb.Append(td.text); // #F 追加文本并记录已使用的空间
					used += td.text.Length;
					break;
				case GroupDoc gd: // #G 必要时将后代可选中断转换为换行符
					docs.Push((gd.Doc.Fits(width - used), indent, gd.Doc));
					break;
				case IndentDoc id: // #H 在后代中任何换行符后增加缩进级别
					docs.Push((fits, indent + IndentSpaces, id.Doc));
					break;
				case OptBreakDoc bd:
					if (fits) // #I 可选中断不转换为换行符
					{
						sb.Append(bd.text);
						used += bd.text.Length;
					}
					else // #J 追加换行符和缩进；重置已用字符计数
					{
						sb.AppendLine();
						sb.Append(' ', indent);
						used = indent;
					}
					break;
				case ConcatDoc cd: // #K 左侧先打印，所以最后推入
					docs.Push((fits, indent, cd.Right));
					docs.Push((fits, indent, cd.Left));
					break;
			}
		}

		return sb.ToString();
	}
}

public sealed record class TextDoc(string text) : Doc;
public sealed record class OptBreakDoc(string text) : Doc;
public sealed record class GroupDoc(Doc Doc) : Doc;
public sealed record class IndentDoc(Doc Doc) : Doc;
public sealed record class ConcatDoc(Doc Left, Doc Right) : Doc;

public sealed record class Id(string Name) : Expr
{
	public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);

	//public override Doc ToDoc()
	//{
	//	throw new NotImplementedException();
	//}
}
public sealed record class Paren(Expr Expr) : Expr
{
	public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
}
public sealed record class Add(Expr Left, Expr Right) : Expr
{
	public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
}
public sealed record class Mult(Expr Left, Expr Right) : Expr
{
	public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
}
public sealed record class Member(Expr Obj, string Name) : Expr
{
	public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
}
public sealed record class Call(Expr Receiver, params IList<Expr> Arguments) : Expr
{
	public override T Accept<T>(IExprVisitor<T> visitor) => visitor.Visit(this);
}

public abstract record class Expr
{
	//public abstract Doc ToDoc();
	public abstract T Accept<T>(IExprVisitor<T> visitor);
}

public interface IExprVisitor<T>
{
	T Visit(Id id);
	T Visit(Paren paren);
	T Visit(Add add);
	T Visit(Mult mult);
	T Visit(Member member);
	T Visit(Call call);
}

class MyExprVisitor : IExprVisitor<int>
{
	public int Visit(Id id)
	{
		throw new NotImplementedException();
	}

	public int Visit(Paren paren)
	{
		throw new NotImplementedException();
	}

	public int Visit(Add add)
	{
		throw new NotImplementedException();
	}

	public int Visit(Mult mult)
	{
		throw new NotImplementedException();
	}

	public int Visit(Member member)
	{
		throw new NotImplementedException();
	}

	public int Visit(Call call)
	{
		throw new NotImplementedException();
	}
}

public sealed class ExprToDoc : IExprVisitor<Doc>
{
	private static readonly Doc comma = Text(","); // #B 为每个标点符号创建可重用的文档
	private static readonly Doc lparen = Text("(");
	private static readonly Doc rparen = Text(")");
	private static readonly Doc star = Text("*");
	private static readonly Doc plus = Text("+");
	private static readonly Doc dot = Text(".");
	public Doc Visit(Id id) => Text(id.Name);

	public Doc Visit(Paren paren) => Group(Indent(lparen + OptBreak + paren.Expr.Accept(this)) + OptBreak + rparen);

	public Doc Visit(Add add) => BinOp(add.Left, plus, add.Right);
	
	private Doc BinOp(Expr left, Doc op, Expr right) => Group(Indent(Group(left.Accept(this) + Space + op) + Space + right.Accept(this)));

	public Doc Visit(Mult mult) => BinOp(mult.Left, star, mult.Right);

	public Doc Visit(Member member) => Group(member.Obj.Accept(this) + Indent(Group(OptBreak + dot + Text(member.Name))));

	public Doc Visit(Call call) => call.Receiver.Accept(this) + ParenList(call.Arguments);

	public Doc CommaList(IList<Expr> items)
	{
		Doc result = Empty;
		for (int i = 0; i < items.Count; i += 1)
		{
			result += items[i].Accept(this);
			if (i != items.Count - 1)
				result += comma + Space;
		}
		return result;
	}

	private Doc ParenList(IList<Expr> items) =>
	items.Count == 0 ?
	lparen + rparen :
	Group(Indent(lparen + OptBreak + CommaList(items) + rparen));
}

public static class Extensions {
	extension(Expr expr) {
		public Doc ToDoc() => expr.Accept(new ExprToDoc());
		public string Pretty(int width) => expr.ToDoc().Pretty(width);
	}
}

