package excs.concurrency;

import concurrency.*;

/**
 * E01_Runable
 */
public class E01_Runable {

    public static void main(String[] args) {
        for (int i = 0; i < 5; i++) {
            new Thread(new Printer()).start();
        }
    }
}