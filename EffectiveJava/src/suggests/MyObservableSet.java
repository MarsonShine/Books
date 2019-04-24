package suggests;

import java.util.*;

/**
 * ObservableSet
 */
public class MyObservableSet<E> extends ForwardingSet<E> {
    public MyObservableSet(Set<E> s) {
        super(s);
    }

    // 可以改用并发库中的 CopyOnWriteArrayList
    private final List<SetObserver<E>> observers = new ArrayList<SetObserver<E>>();

    public void addObserver(SetObserver<E> observer) {
        synchronized (observers) {
            observers.add(observer);
        }
    }

    public boolean removeObserver(SetObserver<E> observer) {
        synchronized (observers) {
            return observers.remove(observer);
        }
    }

    // private void notifyElementAdded(E element) {
    // synchronized (observers) {
    // for (SetObserver<E> observer : observers) {
    // observer.added(this, element);
    // }
    // }
    // }
    // 同步方法内部要避免调用外部方法
    // 最佳做法是把外部方法移到同步区域外
    private void notifyElementAdded(E e) {
        List<SetObserver<E>> snapshot = null;// 建立一个快照
        synchronized (observers) {
            snapshot = new ArrayList<>(observers);
        }
        for (SetObserver<E> observer : snapshot) {
            observer.added(this, e);
        }
    }

    @Override
    public boolean add(E e) {
        boolean added = super.add(e);
        if (added)
            notifyElementAdded(e);
        return added;
    }

    @Override
    public boolean addAll(Collection<? extends E> set) {
        boolean result = false;
        for (E e : set) {
            result |= add(e);
        }
        return result;
    }
}