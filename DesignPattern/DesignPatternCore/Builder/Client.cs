using System;

namespace DesignPatternCore.Builder {
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
}