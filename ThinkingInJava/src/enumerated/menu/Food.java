package enumerated.menu;

/**
 * 使用接口组织枚举 Food
 */
public interface Food {

    enum Appetizer implements Food {
        SALAD, SOUP, SPRING_ROLLS;
    }

    enum MainCourse implements Food {
        LASANGE, BURRITO, PAD_THAI, LENTILS, HUMMOUS, VINDALOO;
    }

    enum Dessert implements Food {
        TIRAMSU, GELATO, BLACK_FOREST_CAKE, FRUIT, CREME_CARAMEL;
    }

    enum Coffe implements Food {
        BLACK_COFFEE, DECAF_COFFEE, ESPRSSO, LATTE, CAPPUCCINO, TEA, HERB_TEA;
    }
}