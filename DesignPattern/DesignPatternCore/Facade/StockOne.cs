using System;

namespace DesignPatternCore.Facade {
    public class StockOne {
        /// <summary>
        /// 卖股票
        /// </summary>
        public void Sell() {
            Console.WriteLine(" 股票 1 卖出");
        }
        /// <summary>
        /// 买股票
        /// </summary>
        public void Buy() {
            Console.WriteLine(" 股票 1 买入");
        }
    }
}