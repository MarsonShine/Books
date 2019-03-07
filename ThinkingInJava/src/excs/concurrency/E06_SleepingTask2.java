package excs.concurrency;

import java.util.concurrent.*;

import concurrency.SleepingTask2;

/**
 * E06_SleepingTask2
 */
public class E06_SleepingTask2 {

    public static void main(String[] args) {
        ExecutorService exec = Executors.newCachedThreadPool();
        if (args.length != 1) {
            System.err.println("Provide the quantity of tasks to run");
            return;
        }
        for (int i = 0; i < Integer.parseInt(args[0]); i++) {
            exec.execute(new SleepingTask2());
        }
        Thread.yield();
        exec.shutdown();
    }
}