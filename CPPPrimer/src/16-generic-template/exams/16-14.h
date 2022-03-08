#include <string>
#include <iostream>

template <unsigned H, unsigned W>
class Screen {
public:
    typedef std::string::size_type pos;
    Screen() = default;
    Screen(char c) : contents(H * W, c) { }
    char get() const { return contents[cursor]; }
    Screen& move(pos r, pos c);

    friend std::ostream& operator<<(std::ostream &os, const Screen<H, W> &c)
    {
        unsigned int i, j;
        for (i=0; i<c.height; i++) {
            os << c.contents.substr(0, W) << std::endl;
        }
        return os;
    }

    friend std::istream& operator>>(std::istream &is, Screen &c)
    {
        char a;
        is >> a;
        std::string temp(H*W, a);
        c.contents = temp; 
        return is;
    }
private:
    pos cursor = 0;
    pos height = H, width = W;
    std::string contents;
};

template<unsigned H, unsigned W>
inline
Screen<H, W>& Screen<H, W>::move(pos r, pos c)
{
    pos row = r * width;
    cursor = row + c;
    return *this;
}