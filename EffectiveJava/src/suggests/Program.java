package suggests;

import java.util.HashSet;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

/**
 * Program
 */
public class Program {
    public static void main(String[] args) {
        MyObservableSet<Integer> set = new MyObservableSet<Integer>(new HashSet<Integer>());
        // set.addObserver(new SetObserver<Integer>() {
        // @Override
        // public void added(MyObservableSet<Integer> set, Integer element) {
        // System.out.println(element);
        // // 增加以下语句会报错
        // if (element == 23)
        // set.removeObserver(this);
        // }
        // });
        // 采用后端线程更改集合状态
        // 会导致死锁
        set.addObserver(new SetObserver<Integer>() {
            @Override
            public void added(MyObservableSet<Integer> set, Integer element) {
                System.out.println(element);
                if (element == 23) {
                    ExecutorService executorService = Executors.newSingleThreadExecutor();
                    final SetObserver<Integer> observer = this;
                    try {
                        executorService.submit(new Runnable() {
                            @Override
                            public void run() {
                                set.removeObserver(observer);
                            }
                        }).get();
                    } catch (ExecutionException ee) {
                        throw new AssertionError(ee.getMessage());
                    } catch (InterruptedException ie) {
                        throw new AssertionError(ie.getMessage());
                    } finally {
                        executorService.shutdown();
                    }
                }
            }
        });
        for (int i = 0; i < 100; i++) {
            set.add(i);
        }
    }
}