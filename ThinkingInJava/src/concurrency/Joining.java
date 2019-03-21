package concurrency;

import static net.mindview.util.Print.*;

class Sleeper extends Thread {
    private int duration;

    public Sleeper(String name, int sleepTime) {
        super(name);
        duration = sleepTime;
        start();
    }

    @Override
    public void run() {
        try {
            sleep(duration);
        } catch (InterruptedException e) {
            print(getName() + " was interrupted. " + "isInterruped(): " + isInterrupted());
            return;
        }
        print(getName() + " has awakend");
    }
}

class Joiner extends Thread {
    private Sleeper sleeper;

    public Joiner(String name, Sleeper sleeper) {
        super(name);
        this.sleeper = sleeper;
        start();
    }

    @Override
    public void run() {
        try {
            sleeper.join();
        } catch (InterruptedException e) {
            print("Interrupted");
        }
        print(getName() + " join completed");
    }
}

/**
 * Joining
 */
public class Joining {
    public static void main(String[] args) {
        Sleeper sleepy = new Sleeper("Sleepy", 1500), grumpy = new Sleeper("grumpy", 15000);
        Joiner dopey = new Joiner("Dopey", sleepy), doc = new Joiner("doc", grumpy);
        grumpy.interrupt();
    }
}