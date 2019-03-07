package concurrency;

import java.util.Random;
import java.util.concurrent.*;

/**
 * SleepingTask
 */
public class SleepingTask2 implements Runnable {
    private static Random rnd = new Random();
    private final int sleep_time = rnd.nextInt(10) + 1;

    @Override
    public void run() {
        try {
            TimeUnit.SECONDS.sleep(sleep_time);
        } catch (InterruptedException e) {
            System.err.println("Interrupted:" + e);
        }
        System.out.println(sleep_time);
    }

    public static void main(String[] args) {
        ExecutorService exec = Executors.newCachedThreadPool();
        for (int i = 0; i < 5; i++) {
            exec.execute(new SleepingTask());
        }
        exec.shutdown();
    }
}