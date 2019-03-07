package excs.concurrency;

import java.util.concurrent.*;
import concurrency.*;

/**
 * E03_Runnable2
 */
public class E03_Runnable2 {

    public static void main(String[] args) {
        ExecutorService exec = Executors.newCachedThreadPool();
        for (int i = 0; i < 5; i++) {
            exec.execute(new Printer());
        }
        Thread.yield();
        exec.shutdown();
    }
}