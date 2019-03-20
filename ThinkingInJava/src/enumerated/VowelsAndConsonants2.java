package enumerated;

import java.util.Random;
import static net.mindview.util.Print.*;

/**
 * VowelsAndConsonants2
 */
public class VowelsAndConsonants2 {

    public static void main(String[] args) {
        Random rand = new Random(47);
        for (int i = 0; i < 100; i++) {
            int c = rand.nextInt(26) + 'a';
            printnb((char) c + ", " + c + ": ");
            print(CharacterCategory.getCategory((char) c).toString());
        }
    }
}