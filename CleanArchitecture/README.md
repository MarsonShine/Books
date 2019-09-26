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

## 

