# 并发编程技巧

## 如何新建一个线程

新建线程 / 任务有集成和实现接口两种方式

- 继承 **Thread**，然后覆写 **run** 方法

```java
public class MyThread extends Thread {
    public MyThread(string name){
        super(name);
        start();
    }
    public void run(){
        try{
            system.out.println("这是一个运行的线程");
        }catch(InterruptedException e){
            system.out.println("线程出错");
        }
    }
}
```

定义了自己的 Thread 之后，调用 **start** 方法开始执行线程的任务，在这里我是直接在构造函数中调用了父类的 start 方法，在初始化  **MyThread** 时就开始执行任务。当然也可以在实例化之后调用

```java
MyThread thread = new MyThread();
thread.start();
```

- 实现 **Runnable**，然后覆写 **run** 方法

```java
public class MyTask implements Runnable {
    private Thread t = new Thread(this);
    public MyTask(){
        t.start();
    }
    public void run(){
        try{
            system.out.println("这是一个运行的线程");
        }catch(InterruptedException e){
            system.out.println("线程出错");
        }
    }
}
```

同样这里是在 MyTask 类内部中申明了一个线程来实际调用要做的事。有人肯定会有疑问了，这不是跟之前的方式1 不是如出一辙么，并且还画蛇添足了。其实在这种方式（实现 Runnable 接口）更多用在线程池上的，这点我们下节会提到。

## 利用线程池新建线程

我们知道新建一个线程的开销是很大的，并且你初始化出来的 Thread 要自己管理这个线程的生命周期，这是非常繁琐且难度较大的。java 于是提供了线程池，来代替我们自动管理每个线程的生命周期，并且也是比我们手动管理性能是要好的。java 提供了 **Executors** 来帮忙更好的管理线程。

```java
public class CachedThreadPool {
    public static void main(String[] args) {
        ExecutorService exec = Executors.newCachedThreadPool();
        for (int i = 0; i < 5; i++) {
            exec.execute(new LiftOff());
        }
        exec.shutdown();
    }
}
```

Executors.newCachedThreadPool 会生成一个 **ExecutorService** 类型的执行器服务，只有调用 **execute** 方法时，才会出发具体的任务，并且这个方法只接受 **Runnable** 类型的参数。

## join，加入一个线程

一个线程运行时可以调用另一个线程的 join 方法。A 线程中调用 B.join() 可以理解为在 A 线程执行过程我要挂起 A 线程转而去执行 B 的线程，等 B 线程结束后在唤起 A 线程来继续执行。看下面例子

```java
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
      	//ta.interrupted();
    }
}

print:
TaskA start running...
TaskB start running...
A awakend
TaskA finished and continu TaskB
B completed
```

其实这个可以用于线程与线程之间的调度，有前后顺序之分的场景。可以更加灵活高效。