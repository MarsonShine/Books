package lexer;

import java.io.IOException;
import java.util.Hashtable;

/**
 * 词法分析器
 */
public class Lexer {
    public int line = 1;
    private char peek = ' ';
    private Hashtable words = new Hashtable<>();
    void reserve(Word t) {
        words.put(t.lexeme, t);
    }
    public Lexer() {
        reserve(new Word(Tag.TRUE, "true"));
        reserve(new Word(Tag.FALSE, "false"));
    }

    public Token scan() throws IOException {
        for(;;peek = (char)System.in.read()) {
            if (peek == ' ' || peek == '\t') {
                continue;
            }
            else if (peek == '\n') {
                line++;
            }
            else {
                break;
            }
        }
        // 处理注释
        if (peek == '/') {
            peek = (char)System.in.read();
            if (peek == '/') {
                do {
                    peek = (char)System.in.read();
                } while (peek != '\n');
            }
            else if (peek == '*') {
                // 处理多行注释
                char prevPeek = ' ';
                do {
                    prevPeek = peek;
                    peek = (char)System.in.read();
                } while (prevPeek != '*' || peek != '/');
            }
            else {
                throw new IOException("illegal comment");
            }
        }
        // 处理关系运算符
        if("<>=!".indexOf(peek) > -1) {
            StringBuffer b = new StringBuffer();
            b.append(peek);
            peek = (char)System.in.read();
            if (peek == '=') {
                b.append(peek);
            }
            return new Rel(b.toString());
        }
        // 读取数位序列
        if (Character.isDigit(peek)) {
            int v = 0;
            do {
                v = 10 * v + Character.digit(peek, 10);
                peek = (char)System.in.read();
            } while (Character.isDigit(peek));
            return new Num(v);
        }
        // 分析保留字和标识符
        if(Character.isLetter(peek)) {
            StringBuffer b = new StringBuffer();
            do {
                b.append(peek);
                peek = (char)System.in.read();
            } while (Character.isLetterOrDigit(peek));
            String s = b.toString();
            Word w = (Word)words.get(s);
            if (w != null) {
                return w;
            }
            w = new Word(Tag.ID, s);
            words.put(s,w);
            return w;
        }
        // 将当前字符作为一个词法单元返回
        Token t = new Token(peek);
        peek = ' ';
        return t;
    }
}
