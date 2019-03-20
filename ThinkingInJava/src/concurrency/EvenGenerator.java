package concurrency;

/**
 * EventGenerator
 */
public class EvenGenerator extends IntGenerator {
    private int currentEventValue = 0;

    public int next() {
        ++currentEventValue;
        ++currentEventValue;
        return currentEventValue;
    }

    public static void main(String[] args) {
        EventChecker.test(new EvenGenerator());
        ;
    }
}