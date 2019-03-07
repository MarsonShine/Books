package excs.concurrency;

import java.util.concurrent.TimeUnit;

import concurrency.LiftOff;

/**
 * E08_MoreBasicThreads
 */
public class E08_MoreBasicThreads {
    // 设置后台线程，main退出，程序立刻结束
    public static void main(String[] args) {
        for (int i = 0; i < 5; i++) {
            Thread t = new Thread(new LiftOff());
            t.setDaemon(true);
            t.start();
        }
        // try {
        // Thread.sleep(1000);
        // } catch (InterruptedException e) {
        // }

        System.out.println("Waiting for LiftOff");
    }
}