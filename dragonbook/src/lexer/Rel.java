package lexer;

/**
 * 关系运算符
 */
public class Rel extends Token {
    public final String lexeme;
    public Rel(String s) {
        super(Tag.REL);
        lexeme = s;
    }
    
}
