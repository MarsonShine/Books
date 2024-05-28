package lexer;
/**
 * 保留字、标识符
 * 比如用于保留字 true 的对象可以初始化: <code>new Word("true", Token.TRUE)</code>
 */
public class Word extends Token {
    public final String lexeme;
    public Word(int t,String s) {
        super(t);
        this.lexeme = s;
    }
}
