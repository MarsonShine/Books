package net.mindview.util;

import java.util.ArrayList;

/**
 * CollectionData
 */
public class CollectionData<T> extends ArrayList<T> {

    private static final long serialVersionUID = 1L;

    public CollectionData(Generator<T> gen, int quantity) {
        for (int i = 0; i < quantity; i++) {
            add(gen.next());
        }
    }

    // 范型约束方法
    public static <T> CollectionData<T> list(Generator<T> gen, int quantity) {
        return new CollectionData<>(gen, quantity);
    }

}