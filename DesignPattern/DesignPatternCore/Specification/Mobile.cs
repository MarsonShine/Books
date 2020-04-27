namespace DesignPatternCore.Specification {
    public class Mobile {
        public string Type { get; set; }
        public decimal Price { get; set; }
        public Mobile(string type, decimal price) {
            Type = type;
            Price = price;
        }
    }
}