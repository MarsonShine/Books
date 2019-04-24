package enumerated;

import net.mindview.util.Enums;

enum Activity {
    SITTING, LYING, STANDING, HOPPING, RUNNING, DOGGING, JUMPING, FALLING, FLYING
}

/**
 * RandomTest
 */
public class RandomTest {
    public static void main(String[] args) {
        for (int i = 0; i < 20; i++) {
            System.out.print(Enums.random(Activity.class) + " ");
        }
    }
}