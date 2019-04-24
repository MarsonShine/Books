package suggests;

import java.util.*;

interface Service {
    // here is specific service method eg.
    String setName();
}

interface Provider {
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
        Provider p = providers.get(name);
        if (p == null)
            throw new IllegalArgumentException("No Provider registered with name: " + name);
        return p.newService();
    }
}