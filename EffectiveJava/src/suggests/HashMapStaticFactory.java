package suggests;

import java.util.HashMap;

/**
 * HashMapStaticFactory
 */
public class HashMapStaticFactory {

    public static <K, V> HashMap<K, V> newInstance() {
        return new HashMap<>();
    }
}