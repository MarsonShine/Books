package concurrency;

import java.util.concurrent.*;

/**
 * ExceptionThread 发生为捕获的异常，以及在main函数下加上 try catch 整个包围都是无法捕获异常信息
 */
public class ExceptionThread implements Runnable {
    @Override
    public void run() {
        throw new RuntimeException();
    }

    public static void main(String[] args) {
        ExecutorService exec = Executors.newCachedThreadPool();
        exec.execute(new ExceptionThread());
    }

}