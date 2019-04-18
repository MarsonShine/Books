package suggests;

import java.util.*;
import java.util.function.Predicate;

/**
 * InstrumentedHashSet
 * 
 * 016 复合优先于继承
 */
public class InstrumentedHashSet<E> extends HashSet<E> {
    // 表示添加的元素个数
    private int addCount = 0;

    public InstrumentedHashSet() {
    }

    public InstrumentedHashSet(int initCap, float loadFactor) {
        super(initCap, loadFactor);
    }

    @Override
    public boolean add(E e) {
        addCount++;
        return super.add(e);
    }

    @Override
    public boolean addAll(Collection<? extends E> c) {
        addCount += c.size();
        return super.addAll(c);
    }

    public int getAddCount() {
        return addCount;
    }
}
