package enumerated;

import java.util.EnumMap;
import java.util.Map;

import static net.mindview.util.Print.*;
import static enumerated.AlarmPoints.*;

interface Command {
    void action();
}

/**
 * EnumMaps
 */
public class EnumMaps {

    public static void main(String[] args) {
        EnumMap<AlarmPoints, Command> em = new EnumMap<>(AlarmPoints.class);
        em.put(KICHEN, new Command() {
            @Override
            public void action() {
                print("Kitchen fire!");
            }
        });
        em.put(BATHROOM, new Command() {
            @Override
            public void action() {
                print("Bathroom alert!");
            }
        });
        // since java 1.2
        for (Map.Entry<AlarmPoints, Command> e : em.entrySet()) {
            printnb(e.getKey() + ": ");
            e.getValue().action();
        }
        for (Map<AlarmPoints, Command>.Entry e : em.entrySet()) {
            printnb(e.getKey() + ": ");
            ((Command) e.getValue()).action();
        }
    }
}