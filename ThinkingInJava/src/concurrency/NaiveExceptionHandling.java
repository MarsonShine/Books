package concurrency;

import java.util.concurrent.*;

/**
 * NaiveExceptionHandling
 */
public class NaiveExceptionHandling {

    public static void main(String[] args) {
        try {
            ExecutorService exec = Executors.newCachedThreadPool();
            exec.execute(new ExceptionThread());
        } catch (RuntimeException e) {
            // 这段代码无法执行
            System.err.println("Exception has been handled!");
        }
    }
}