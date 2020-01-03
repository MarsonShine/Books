namespace DesignPatternCore.Facade {
    public class Found {
        private StockOne stockOne;
        private StockTwo stockTwo;
        private StockThree stockThree;
        private NationalDebt nationalDebt;
        private Realty realty;
        public Found() {
            stockOne = new StockOne();
            stockTwo = new StockTwo();
            stockThree = new StockThree();
            nationalDebt = new NationalDebt();
            realty = new Realty();
        }

        public void Buy() {
            stockOne.Buy();
            // ..stockTwo/stockThree/nationalDebt/realty.Buy();
        }

        public void Shell() {
            stockOne.Sell();
            // ..stockTwo/...
        }
    }
}