package suggests;

/**
 * SetObserver
 */
public interface SetObserver<E> {
    void added(MyObservableSet<E> set, E element);
}