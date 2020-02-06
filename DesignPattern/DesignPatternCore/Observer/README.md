# 如何重构我们以前写的垃圾代码——观察者模式

首先来看下 GoF 对观察者模式的定义：

> 多个对象间存在一对多关系，当一个对象发生改变时，把这种改变通知给其他多个对象，从而影响其他对象的行为

就是说当一个对象要发生变化时，要通知其他多个对象同时要发生相应的变化的行为。

从这句定义上来看，重点在于两个“对象”——观察者（后者多个对象），被观察者（前者一个对象）。也就是我们经常说的订阅者和发布者。

首先我们先用最直观的代码来实现上面的这句话。

以我们高三高考前夕为时间背景，为了缓建我们的学习压力，我们班的搞事同学们总是会和当天的值日生串通一气，在最后一节自习课里，值日生在教室外放哨，我们在教室里看电影《肖生克的救赎》（哈哈哈哈哈，应该不止我们这么干过哈～

```c#
// 值日生
public class StudentOnDuty {
    private List<StudentObserver> observers = new List<StudentObserver>();
    public void Notify() {
        foreach (var observer in observers) {
            observer.Update();
        }
    }

    public void Attach(StudentObserver observer) {
        observers.Add(observer);
    }

    public string State => "班主任来了！！！";
}

// 搞事的同学们
public class StudentObserver {
    private readonly StudentOnDuty _studentOnDuty;
    public StudentObserver(string studentTypeName, StudentOnDuty studentOnDuty) {
        _studentOnDuty = studentOnDuty;
        StudentTypeName = studentTypeName;
    }
    public void Update() {
        Console.WriteLine(StudentTypeName + "接受到了来自值日生的通知：" + _studentOnDuty.State+ " 关闭讲台的电脑并假装翻开书本看书，写作业等");
    }

    public string StudentTypeName { get; }
}
// Program
StudentOnDuty studentOnDuty = new StudentOnDuty();
studentOnDuty.Attach(new StudentObserver("marson shine", studentOnDuty));
studentOnDuty.Attach(new StudentObserver("summer zhu", studentOnDuty));
studentOnDuty.Notify();
```

这段代码非常简单，也是最能体现出对观察者概念定义的。

那么上面这段代码有什么问题呢？其实很明显，我们看到值日生和搞事的同学们这两个类是直接耦合的（值日生要一个个通知他 Attach 的同学，而同学还要记得值日生传过来的状态），那么这就意味着只要改动其中一个类，受影响的就是整体的逻辑。比如我们有些童鞋是不喜欢看我们这次播放的电影，那么他们必定会干其它事，比如玩手机，看小说等。所以针对 `StudentObserver.Update()` 就要更改。

# 如何重构？

一提到重构，我们脑子里就要闪出一个念头，封装公共部分，提取抽象部分来应对变化的部分。

开闭原则告诉我们对修改关闭，对新增开放。

依赖倒置原则告诉我们要依赖于抽象而不是具体实现。

所以第一件事我们就是要抽象，提取公共部分。显然，值日生作为通知者，它的通知行为是稳定的。所以把它抽象成一个接口

```c#
public interface ISubject {
  	object State { get;}
		void Notify();
}
```

那么值日生就得继承它来称为一个通知者，有人可能会说这里的场景没必要让值日生抽象，因为它是稳定的，它只负责同志我们嘛。其实不然，值日生不可能无时无刻的站在教室外帮我们放哨，不然那也太明显了吧～。～

值日生有时候也会跟我们一起看电影中的高潮部分啊哈哈。那么正当这时候，我们的班主任进教室了，除了突然变得死静以及空气都凝固的教室之外，我们拿着通知者身份牌的是不是由值日生换成班主任了啊！所以观察者肯定也是要抽象出来的。而观察者抽象出来的职责只有一个——接受通知者的通知，来作出相应的更新。

```c#
public interface IObserver {
		void Update();
}
```

那么如何给值日生添加订阅者，以及通知呢。因为添加的对象会变，是不稳定的，所以我们要做出相应措施来应对这种情况。

就从原来直接的添加观察对象转变成了"面向接口"编程——首先把代表被通知的群体并做出相应改变的行为抽象出来成一个接口，它就只有一个职责，就是做出变换，比如关闭电脑，打开书本假装自习等。

```c#
public class StudentOnDuty: ISubject {
		private readonly List<IObserver> _observers = new List<IObserver>();
  
  	public void AddObserver(IObserver observer)
    {
      	_observers.Add(observer);// 简单起见，不验重等判断
    }
		public void Notify()
		{
				....
		}
  	public object State => "关电脑，老师来了！！！";
}

public class ClassTeacher: ISubject {
  	...
    public object State => "就怕突如其来的安静！！！";
  	...
}

```

那么我们的观察者们同样接受到通知者的讯息，也不能具体的，也要面向抽象编程。因为我们不知道我们即将面对的是没事的值日生通知还是暴雷的班主任的通知。

```c#
// 看电影的同学
public class MovieStudent: IObserver {
  	private readonly ISubject _subject;
  	public MovieStudent(ISubject subject) {
      	_subject = subject;
    }
  	public void Update()
    {
      	Console.WriteLine($"看电影的同学接受到了subject的讯号:{_subject.State},立马打开书本假装自习。")
    }
}
// 负责关电脑的同学
public class ClosePlayerStudent: IObserver {
  	//...
}
```

这样我们的客户端的代码跟之前的调用无异，当要添加干其他事的同学的通知者时，我们客户端代码也无需做过多改变，只需增加具体的实现类即可。

# 从另一个角度优化

看到这里千万不要满意这个时候的重构程度，其实继续看我们就会发现，我们在面向接口编程的时候，那我们就必须得按照接口的契约来实现，特别是对代码有洁癖的人来说，这些观察者的实现类的方法“Update”一词实在是不妥，概念太大，太泛了。特别是在这个微服务盛行的时代，每个方法代表的职责要求非常明确，并且我们要从名字上一眼就能看出其行为是最好的。比如负责关电脑的同学他的“Update”所做的就是关电脑并打开书本看书，那么方法名就应该改为`ClosePlayerThenOpenTheBook()`，看电影的同学就只需要直接打开书本看书即可，即`OpenTheBook()`。

那么在这个时候，我们如何通知他们并做出相应的行为呢？

这个时候，我们的委托就要出场了，我们可以给通知者暴露一个委托（也可以是事件），让这个委托来执行具体观察者的更新行为。

```c#
public class ClassTeacher
{
  	...
  	public Action UpdateEvent { get; set;}
  	public void Update()
    {
      	UpdateEvent?.Invoke();
    }
  	...
}
```

那么具体的行为操作就转到了客户端了

```c#
classTeacher.UpdateEvent += closePlayerStudent.ClosedPLayerThenOpenTheBook;
classTeacher.UpdateEvent += moiveStudent.OpenTheBook;
```

为什么说这是从另一个角度呢，因为这种实现方式其实跟观察者模式没什么关系了，因为我们可以去除掉这些接口也能做到。这种做法我们做到了将观察者和通知者行为分离。

# 订阅-发布模式

刚开始我以为订阅-发布模式就是观察者模式，只是说法不同而已。

但是实际上呢，他们其实还是一个东西......嗯～～是的，没错。

我在网上也查看了一些资料，发现其实有一些人将订阅-发布模式与观察者模式区别开了。说现在我们平时讲的订阅-发布模式与观察者模式不一样，前者能做到完全隔离，零耦合。比如消息中间件等。

其实它们的思想还是一致的，只是订阅发布模式增加一个消息调度层用来传递信息，使之订阅者和发布者不知道彼此的存在。**但还是通过一端的改变，其他依赖的对象都会做出相应的更新。**

