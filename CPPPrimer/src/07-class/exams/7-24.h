#ifndef _7_24_h
#define _7_24_h

#include <string>
class Screen {
    public:
        using pos = std::string::size_type;

        Screen() = default;
        Screen(pos w, pos h) : width(w), height(h), contents(w*h, ' ') { }
        Screen(pos w, pos h, char c) : width(w), height(h), contents(w*h, c) { }
        char get() const { return contents[cursor]; }
        char get(pos r, pos c) const { return contents[r*width+c]; }

    private:
        pos cursor = 0;
        pos height = 0, width = 0;
        std::string contents;
};
#endif