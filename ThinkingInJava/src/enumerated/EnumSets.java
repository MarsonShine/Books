package enumerated;

import java.util.EnumSet;

import static net.mindview.util.Print.*;

import static enumerated.AlarmPoints.*;

/**
 * EnumSets
 */
public class EnumSets {

    public static void main(String[] args) {
        EnumSet<AlarmPoints> points = EnumSet.noneOf(AlarmPoints.class);
        points.add(BATHROOM);
        print(points);
        points.addAll(EnumSet.of(STAR1, STAR2, KITCHEN));
        print(points);
        points = EnumSet.allOf(AlarmPoints.class);
        points.removeAll(EnumSet.of(STAR1, STAR2, KITCHEN));
        print(points);
        points.removeAll(EnumSet.range(OFFICE1, OFFICE4));
        print(points);
        points = EnumSet.complementOf(points);
        print(points);
    }
}