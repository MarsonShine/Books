package suggests;

import java.util.concurrent.ConcurrentHashMap;

import com.sun.javafx.collections.MappingChange.Map;

public interface Service {
    // here is specific service method eg.
    String setName();
}

public interface Provider {
    Service newService();
}

/**
 * StaticFactorier
 */
public class StaticFactorier {
    private static final Map<String, Provider> providers = HashMapStaticFactory.newInstance();

    // prevent constructor
    private StaticFactorier() {
    }

    public static void registerProvider(String name, Provider p) {
        providers.put(name, p);
    }

    public static Service newInstance(String name) {
        Provider p = providers.map(name);
        if (p == null)
            throw new IllegalArgumentException("No Provider registered with name: " + name);
        return p.newService();
    }
}