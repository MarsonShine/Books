package excs.concurrency;

class TaskA extends Thread {
    private final int sleepTime;

    public TaskA(String name, int sleepTime) {
        super(name);
        this.sleepTime = sleepTime;
        start();
    }

    @Override
    public void run() {
        try {
            System.out.println("TaskA start running...");
            sleep(sleepTime);
        } catch (InterruptedException e) {
            System.err.println(getName() + " interrupted!!");
            return;
        }
        System.out.println(getName() + " awakend");
    }
}

class TaskB extends Thread {
    private TaskA ta;

    public TaskB(String name, TaskA ta) {
        super(name);
        this.ta = ta;
        start();
    }

    @Override
    public void run() {
        try {
            System.out.println("TaskB start running...");
            ta.join();// 等待ta的结束
            System.out.println("TaskA finished and continu TaskB");
        } catch (InterruptedException e) {
            System.err.println("interrupted!");
        }
        System.out.println(getName() + " completed");
    }
}

/**
 * E01_Jonning
 */
public class E01_Jonning {
    public static void main(String[] args) {
        TaskA ta = new TaskA("A", 2000);
        TaskB tb = new TaskB("B", ta);

    }
}