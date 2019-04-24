package annotations;

import java.lang.annotation.*;

/**
 * UseCase 注解定义
 */
@Target(ElementType.METHOD)
@Retention(RetentionPolicy.RUNTIME)
public @interface UseCase {
    public int id();

    public String description() default "no description";
}