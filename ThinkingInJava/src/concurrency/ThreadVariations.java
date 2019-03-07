package concurrency;

import static net.mindview.util.Print.*;

//使用内部类来隐藏线程代码
class InnerThread1 {
    private int countDown = 5;
    private Inner inner;

    private class Inner extends Thread {
        Inner(String name) {
            super(name);
            start();
        }

        @Override
        public void run() {
            try {
                while (true) {
                    print(this);
                    if (--countDown == 0)
                        return;
                    sleep(10);
                }
            } catch (InterruptedException e) {
                print("interrupted");
            }
        }

        @Override
        public String toString() {
            return getName() + ": " + countDown;
        }

        public InnerThread1(String name){
            inner = new Inner(name);
        }
    }
}

// 使用匿名内部类
class InnerThread2 {
    private int countDown = 5;
    private Thread t;

    public InnerThread2(String name) {
        t = new Thread(name) {
            @Override
            public void run() {
                try {
                    while (true) {
                        print(this);
                        if (--countDown == 0)
                            return;
                        sleep(10);
                    }
                } catch (InterruptedException e) {
                    print("sleep() interrupted");
                }
            }

            @Override
            public String toString() {
                return getName() + ": " + countDown;
            }
        };
        t.start();
    }
}

/**
 * ThreadVariations
 */
public class ThreadVariations {

}