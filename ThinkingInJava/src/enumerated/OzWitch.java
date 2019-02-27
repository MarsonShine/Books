package enumerated;

import static net.mindview.util.Print.*;

/**
 * OzWitch 在枚举类添加方法，必须在枚举实例项的最后面要打个分号 才能在后面接自定义方法以及main方法
 */
public enum OzWitch {
    // 枚举注释的方式
    WEST("西方"), NORTH("北方"), EAST("东方"), SOUTH("南方");
    private String description;

    private OzWitch(String description) {
        this.description = description;
    }

    public String getDescription() {
        return description;
    }

    public static void main(String[] args) {
        // OzWitch w = OzWitch.valueOf("WEST");
        OzWitch w = OzWitch.valueOf(OzWitch.class, "EAST");
        print(w);
        for (OzWitch witch : OzWitch.values()) {
            print(witch + ": " + witch.getDescription());
        }
    }

}
