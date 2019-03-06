package concurrency;

/**
 * MainThread
 */
public class MainThread {

    public static void main(String[] args) {
        LiftOff launch = new LiftOff();
        launch.run(); // 直接这样使用 不会开启线程，而是要配合Thread
    }
}