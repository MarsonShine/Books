import java.io.IOException;

/*
 * 将中缀表达式翻译为后缀表达
 */
public class Postfix {
    public static void main(String[] args) throws IOException {
        Parser parser = new Parser();
        parser.expr();
        System.out.println('\n');
    }
}
