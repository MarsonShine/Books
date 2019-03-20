package annotations.database;

import java.lang.annotation.*;

/**
 * DBTable
 */
@Target(ElementType.TYPE)
@Retention(RetentionPolicy.RUNTIME)
public @interface DBTable {
    public String name() default "";
}