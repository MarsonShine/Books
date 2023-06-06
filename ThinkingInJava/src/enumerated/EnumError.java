package enumerated;
import static net.mindview.util.Print.*;

enum LikesClass {
    ITEM1 { void action(){ 
        print("action1");
        print(state);
    }},
    ITEM2 { void action(){ 
        print("action2");
        state = 2;
    }},
    ITEM3 { void action(){ print("action3");}};
    abstract void action();
    public int state = 0;
}

class NotPromise {
    void method(LikesClass instance){
        instance.action();
    }
}

public class EnumError {
    public static void main(String[] args) {
        LikesClass instance = LikesClass.ITEM2;
        instance.action();
        NotPromise notPromise = new NotPromise();
        notPromise.method(instance);
        print(instance.state);

        LikesClass instance1 = LikesClass.ITEM1;
        instance1.action();
    }
}
