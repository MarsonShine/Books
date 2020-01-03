# 装饰者模式（Facade）

装饰者主要是降低不同模块之间耦合。

装饰者现在很像客服电话功能。当我们打电话过去咨询人工服务，但是我们总是先经过一个电话转播，系统会提示你拨打数字1是转售前，数字2转咨询，数字3转售后，等等。说实话这种模式很烦，因为我们就想第一时间找到人工，而不是根据提示去转对应的线路。

但是装饰者实际上就是这种情况，装饰者提供一个统一的入口（界面），然后内部负责转不同的功能。这样才能降低客户端与业务服务的耦合。降低客户端的使用复杂度（无需关注众多烦炸的业务逻辑）

这种特别适合于新老系统交互，在我们的老系统中，由于公司高速发展，以前的业务已经不适合现在公司的需要，需要迎合新的业务做新的需求业务，但是以前的业务不能受影响。且针对于用户操作来说跟以前没变化。这个时候我们就可以很好的运用装饰者模式。

我们新建一个业务B类去处理逻辑B。然后新建一个装饰者类Facade，用户点击按钮，触发业务流程。

```c#
// 老业务逻辑
public class BusinessA {
  	public void HandleA(){}
}
// 新业务逻辑
public class BusinessB {
  	public void HandleB(){}
}
// 外观装饰
public class Facade {
  	private BusinessA ba;
  	private BusinessB bb;
  	public Facade() {
      	ba = new BusinessA();
      	bb = new BusinessB();
    }
  
  	public void Execute() {
      	ba.HandleA();
      	bb.HandleB();
    }
}
```

这样业务A于业务B就解藕了，弱化了两者关系。具体怎么执行，由外观者决定。