package concurrency;

/**
 * Printer
 */
public class Printer implements Runnable {

    private static int taskCount;
    private final int id = taskCount++;

    public Printer() {
        System.out.println("Printer started, ID=" + id);
    }

    public void run() {
        System.out.println("Stage 1..., ID = " + id);
        Thread.yield();// 线程告诉CPU我这个已经执行完生命周期中最重要的部分了，可以切换其它线程了。
        System.out.println("Stage 2..., ID=" + id);
        Thread.yield();// 线程告诉CPU我这个已经执行完生命周期中最重要的部分了，可以切换其它线程了。
        System.out.println("Stage 3..., ID=" + id);
        Thread.yield();// 线程告诉CPU我这个已经执行完生命周期中最重要的部分了，可以切换其它线程了。
        System.out.println("Pinter ended, ID=" + id);
    }
}