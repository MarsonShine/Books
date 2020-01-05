# 建造者模式(Builder)——从组装电脑开始

建造者模式概括起来就是将不同独立的组件按照一定的条件组合起来构成一个相对业务完整的对象。调用者无需知道构造的过程。

# 我们从组装电脑开始

让我们从买组装电脑开始吧。

首先要买一个电脑，一般都有两个选择 —— 品牌电脑和组装电脑，一般人为了省事和放心都会选择买品牌电脑（也就是整机）。在这里，为了更好的分析问题，假定我们为了性价比决定要买组装电脑。那么我们该怎么做呢。

首先我们得学习一个完整的电脑的组成部分有哪些？

经过翻查一部分资料发现，主要部件分为主板、CPU、显卡、显示屏、内存条等。这还刚开始，我们光知道这个还不行，每个硬件的品牌少说都有好几种，我们肯定希望在价格允许的情况选最好的。所以我们还得花时间找资料了解每个部件对应的品牌的口碑与使用者的实际体验情况（比如淘宝的卖家秀，以及网上一些专业的测评人的报告等）。

好了，研究完这个之后呢，总算是可以决定怎么搭配各种硬件了。但是最后又有一个问题来了，这些硬件买回来了，我们这些小白不会装啊，都不知道主机箱里每个部件对应的位置是哪里，以及怎么装上去。万一装坏了怎么办，那钱岂不是白花了。

又是一番曲折之后，总算是把整个电脑组装完毕了。就在我们举杯同庆的时候，突然发现了一个严重的问题……

“天呐，还开不了机啊，还要给电脑装系统啊”

“啊！！！那怎么办啊，都忙活一整天了。”

“还能怎么办啊？买都买了，装都装好了，只能装系统呀”

“也是，可我不会装系统啊，你会么？”

“……嗯～我也不会”

“哎，咱们还是去查资料怎么装系统吧”

上面的故事虽然在我们ITer来看，显得很搞笑和夸张。但是对于电脑小白来说，这可是绝对会发生的。因为在大学，我就是这么过来的。光怎么重装系统都花了我一天时间。

但是，别急，我们隔壁寝室的一个同学他早上说他也要买组装电脑。我们现在去看他怎么弄的吧，说实话，我既希望他也跟我们一样经过种种折磨，但是又希望他也能一帆风顺，这样就能教我们怎么装系统了啊。

# 什么？已经开始玩电脑了

当我们进寝室门的时候，令我们目瞪口呆的事情发生了，我发现他已经开始玩英雄联盟了，都已经三杀并成功结束游戏。

我马上就问他怎么这么快就玩上电脑了，然后我就把我一整天的遭遇发泄了出来。只见他哈哈大笑

”现在谁还自己买各种部件来装电脑啊，更何况像你们还不懂这些。“

”那不然怎么弄，品牌机同等价位的比组装机要贵好多啊“

”哈哈，你这个蠢嘛批，你可以去电脑城让老板帮你组装不就行了，你只需要为此付一些手工费就行了嘛，也不贵啊“

”……“

# 开始对号入座

第一则故事其实就相当于我们没有用建造者模式开发可能面临的一些问题。为了生成一个业务对象（组装电脑），我们得花很多时间精力来收集业务对象的成员信息（组成部分）。这么多对象全由我一个人（客户端）组织，这样就会强耦合，并很有可能因为一些细小需求的改变而导致整个功能异常（忘记装系统，内存条型号不对等）。从而浪费了更多的时间和精力，增加了我们的劳动成本和经济成本。

第二则故事就完全不同，我（用户/客户端）完全不需关心业务对象的构建过程，只需要找电脑城老板（构建者）要对象就行了。

首先有一个前提，就是有一个规则依据（契约）来构造一个正确的业务对象（电脑）。

所以为了以正确姿势来组装电脑，我们定义了一些必要的成员（硬件）

```c#
public interface IFullComputer {
    string Mainboard { get; }
    string CPU { get; }
    string Disk { get; }
    string Graphics { get; }
    string Display { get; }
    bool HasOperatingSystem { get; }
}
```

有了它才代表一个完整正确的电脑，我们先来看我们第二个故事是怎么实现结果调用的吧。

```c#
public class Client {
    // 交给电脑城老板
    private void IWantBuyComputer() {
        // 见到老板
        var boss = new ComputerCityBoss();
        // 告诉老板我想要什么配置的电脑,这里简单起就用老板推荐的
        var computerBuilder = new DefaultFullComputerBuilder();
        var computer = boss.TellMeThenReturnComputer(computerBuilder);
        Console.WriteLine("电脑组件完毕，是否预装系统：" + computer.HasOperatingSystem);
    }

}
```

客户端（用户）已经很简单了，就跟我们现在很多人买电脑一样，去电脑城把自己搭配的电脑配置给老板，然后就等着老板把组装好的电脑交给你。你根本不需要知道电脑组装的细节。这样从代码上就能做客户端与业务数据分离。

现在我们来看具体实现代码。

```c#
public interface IFullComputerBuilder : IFullComputer {
    IFullComputer Create();
}
public class DefaultFullComputerBuilder : AbstractFullComputerBuilder {
    protected override void SetCPU() {
    }

    protected override void SetDisk() {
    }

    protected override void SetDisplay() {
    }

    protected override void SetGraphics() {
    }

    protected override void SetMainboard() {
    }
}
// 老板与品牌商有合作
public abstract class AbstractFullComputerBuilder : IFullComputerBuilder {
    public string Mainboard { get; set; } = "默认品牌主板";
    public string CPU { get; set; } = "默认品牌CPU";
    public string Disk { get; set; } = "默认品牌内存";
    public string Graphics { get; set; } = "默认品牌显卡";
    public string Display { get; set; } = "默认品牌显示器";
    public bool HasOperatingSystem { get; set; }

    public IFullComputer Create() {
        SetMainboard();
        SetCPU();
        SetDisk();
        SetDisplay();
        SetGraphics();
        InstallOperatingSystem();
        if (!HasOperatingSystem) throw new InvalidOperationException("install faild: no operating system");
        return this;
    }

    protected abstract void SetMainboard();
    protected abstract void SetCPU();
    protected abstract void SetDisk();
    protected abstract void SetGraphics();
    protected abstract void SetDisplay();

    private void InstallOperatingSystem() {
        //if (!condition) return;
        HasOperatingSystem = true;
    }
}
```

老板就会根据你的要求来给你组装电脑。当然，如果你没有特殊要求，那老板就会默认用品牌合作商的，利润更多嘛。

```c#
public class ComputerCityBoss {
    public IFullComputer TellMeThenReturnComputer(IFullComputerBuilder builder) {
        return builder.Create();
    }
}
```

