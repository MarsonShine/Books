package enumerated.menu;

import static enumerated.menu.Food.*;

import static net.mindview.util.Print.*;

/**
 * TypeFood
 */
public class TypeOfFood {

    public static void main(String[] args) {
        Food food = Appetizer.SALAD;
        food = MainCourse.LASANGE;
        food = Dessert.GELATO;
        food = Coffe.CAPPUCCINO;
        print(food);
    }
}