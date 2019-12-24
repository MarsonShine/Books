package suggests;

/**
 * BuilderPattern 当这个有很多参数时，同时有要求要有线程安全，是函数式的 并且不影响程序的可读性和维护性，可以使用构造器模式
 */
public class BuilderPattern {
    private final int servingSize;
    private final int servings;
    private final int calories;
    private final int fat;
    private final int sodium;
    private final int carbohydrate;

    public BuilderPattern(Builder builder) {
        servings = builder.servings;
        servingSize = builder.servingSize;
        calories = builder.calories;
        fat = builder.fat;
        sodium = builder.sodium;
        carbohydrate = builder.carbohydrate;
    }

    public static class Builder {
        private final int servingSize;
        private final int servings;
        // 可选参数，可以初始化默认值
        private int calories = 0;
        private int fat = 0;
        private int sodium = 0;
        private int carbohydrate = 0;

        public Builder(int servingSize, int servings) {
            this.servings = servings;
            this.servingSize = servingSize;
        }

        public Builder calories(int val) {
            calories = val;
            return this;
        }

        public Builder carbohydrate(int val) {
            carbohydrate = val;
            return this;
        }

        public Builder sodium(int val) {
            sodium = val;
            return this;
        }

        public Builder fat(int val) {
            fat = val;
            return this;
        }

        public BuilderPattern build() {
            return new BuilderPattern(this);
        }
    }

    public static void main(String[] args) {
        BuilderPattern bp = new BuilderPattern.Builder(240, 8).calories(100).sodium(35).carbohydrate(27).build();
    }

}

interface Builder<T> {
    public T build();
}