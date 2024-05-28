package lexer;
/**
 * 数字，常量
 */
public class Num extends Token {
    public final int value;
    public Num(int v) {
        super(Tag.NUM);
        value = v;
    }
    
}
