package net.mindview.util;

/**
 * Generated
 */
public class Generated {

    public static <T> T[] array(T[] a, Generator<T> gen) {
        return new CollectionData<T>(gen, a.length);
    }
}