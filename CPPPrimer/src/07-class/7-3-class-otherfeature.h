#ifndef _7_3_class_otherfeature_h
#define _7_3_class_otherfeature_h

#include <istream>
#include <string>

class Screen {
public:
    // typedef std::string::size_type pos;
    // 等价于下面
    using pos = std::string::size_type;
    // 定义构造函数
    Screen() = default;
    Screen(pos ht, pos wd, char c) : height(ht), width(wd), contents(ht * wd, c) {}
    char get() const { return contents[cursor]; }   // 读取光标处的字符
    inline char get(pos ht, pos wd) const;  // 显式内联
    Screen &move(pos r, pos c); // 之后可以手动设为内联
    void some_member() const;
    Screen &set(char);
    Screen &set(pos, pos, char);

    // 显示
    Screen &display(std::ostream &os) { do_display(os); return *this;}
    const Screen &display(std::ostream &os) const { do_display(os); return *this;}
private:
    pos cursor = 0;
    pos height = 0, width = 0;
    std::string contents;
    // 可变数据成员
    mutable size_t access_ctr;
    Screen &do_display(std::ostream &os) const { os << contents; }
};
// 类外部可以定义内联函数
inline
Screen &Screen::move(pos r, pos c)
{
    pos row = r * width;
    cursor = row + c;
    return *this;
}
char Screen::get(pos r, pos c) const
{
    pos row = r * width;
    return contents[row + c];
}
// 可变数据成员
void Screen::some_member() const
{
    ++access_ctr;
}
inline Screen &Screen::set(char c)
{
    contents[cursor] = c;
    return *this;
}
inline Screen &Screen::set(pos row, pos col, char c)
{
    contents[row*width + col] = c;
    return *this;
}

// class Window_mgr {
// private:
//     // 默认情况下，一个 Window_mgr 包含一个标准尺寸的空白 Screen
//     std::vector<Screen> screens{Screen(24, 80, ' ')};
// };
#endif