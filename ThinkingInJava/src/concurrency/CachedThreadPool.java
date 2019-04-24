package concurrency;

import java.util.concurrent.*;

/**
 * CachedThreadPool 自由分配线程 FixedThreadPool 固定指定数量的线程 SingleThreadExecutor 单线程
 */
public class CachedThreadPool {

    public static void main(String[] args) {
        ExecutorService exec = Executors.newCachedThreadPool();
        for (int i = 0; i < 5; i++) {
            exec.execute(new LiftOff());
        }
        exec.shutdown();
    }
}