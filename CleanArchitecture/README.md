# 架构整洁之道

## 五大设计原则 SOLID

- SRP:：单一职责原则（Single Responsibility Principle）
- OCP：开闭原则（Open Closure Principle）
- LSP：里氏替换（Liskov Substitution Principle）
- ISP：接口隔离原则（Interface Segregation Principle）
- DIP：依赖反转原则（Dependency Inversion Principle）

## UML 类图

- 矩形框：代表一个类，分三层：第一层类名；第二层类的特性，字段和属性；第三层为类的方法和行为

  ‘+’ 表示 public，‘-’ 表示 private，‘#’ 表示 proteced

- 空心三角形 + 实线：表示继承关系，如 鸟 -▷ 动物 表示 “鸟” 继承自 “动物”。

- 空心三角形 + 虚线：表示接口实现，如 鸟 ---▷ 飞翔，则是表示 “鸟” 实现了 “飞行” 这个接口。

- 实现：表示关联，比如 “大雁南飞” 是因为要过冬，有 “气候” 这么一个类，大雁需要知道 “气候” 的具体变化才会 “南飞”，所以可以用关联关系表示 “一个类知道另一个”；大雁 ——> 气候。

  ```c#
  public class Goose
  {
      private Climate climate;	//这就是关联关系，Goose 类引用 Climate 类
  }
  ```

- 空心菱形 + 实线箭头：表示聚合关系，比如大雁是群居动物，一个大雁属于一个雁群，一个雁群有个大雁。它们满足聚合关系，这种聚合关系是表示一种“弱拥有”，即 A 对象包含 B 对象，但 B 对象不是 A 对象的一部分。

  雁群 ◇—— 大雁。

  ```c#
  public class GooseAggregate
  {
      private Goose[] gooses;	// 在 GooseAggregate 类中，有大雁数组对象 gooses
  }
  ```

- 实心菱形 + 实线箭头：表示组合（composition）关系，是一种 “强拥有” 的关系，体现了严格的部分与整体的关系，部分与整体的生命周期一样。比如，鸟和翅膀就是组合关系；鸟 ◆——> 翅膀；

  ```c#
  public class Bird
  {
      private Wing wing;
      public Bird()
      {
          wing = new Wing();	// 生命周期与 Bird 一样，初始化时同时生成
      }
  }
  ```

- 虚线箭头：表示依赖关系，如动物依赖空气呼吸存活，依赖水源存活等。这属于依赖关系；氧气 <------ 动物；水源<------ 动物；

  ```c#
  abstract class Bird
  {
      public Metabolism (Oxygen oxygen, Water water)
      {
          //...
      }
  }
  ```

## 软件设计的相关原则

- DRY（Don't Repeat Yourself）：意味着，当有多个（一般指 2 个以上）地方发现有相似代码的时候，我们就需要把它们的共性之处抽象出来形成一个方法。
- KISS（Keep It Simple，Stupid）：大道至简。
- YAGNI（You Ain't Gonna Need It）：意思就是说不需要的就不要过多考虑，避免过度设计。等后面需要更多功能时，再添加进来。
- LoD（Law of Demeter）：迪米特法则又称“最少知识原则”。
- CoC（Convention over Configuration）：惯例大于配置原则，就是说将一些公认的规则、配制方法和信息作为内部默认规则来使用。
- Soc（Separation of Concerns）：关注点分离原则，该原则渗透在各个领域，即通过各种手段，我们将一个大问题分解成独立且小的问题，这样就相对容易解决。

## 组件聚合的三个原则

- REP（复用发布等同原则）
- CCP（共同闭包原则）：将同时修改，并且是为了相同目的的修改的类放到同一个组件中，将不会同时修改的，且修改的目的不同的类拆分到不同的组件中去。
- CRP（共同复用原则）：不要依赖不需要的东西（类和方法）

## 组件耦合

- ADP Acyclic Dependency Principle（无依赖环原则）

- DIP Dependent inversion of control（依赖反转原则）

- SDP Stabilization Dependency Principle（稳定依赖原则）

- SAP Stabilization Abstract Principle（稳定抽象原则）


## 软件构建的三个过程

- 先让代码工作起来 — — 如果代码部工作，那就不能产生价值
- 然后再试图将它变好 — — 通过对代码进行重构，让我们自己人和其他人更好的理解代码，并能按照需求不断的修改代码
- 最后再试着让它变得更快 — — 按照性能提升的 “需求” 来重构代码