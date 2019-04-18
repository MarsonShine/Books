package suggests;

import java.util.*;

/**
 * ObservableSet
 */
public class ObservableSet<E> extends ForwardingSet<E> {
    public ObservableSet(Set<E> s) {
        super(s);
    }
}