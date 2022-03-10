#include <type_traits>

namespace template2
{
    template<typename It> auto fcn(It beg, It end) -> decltype(*beg)
    {
        return *beg;
    }

    template<typename It> auto fcn2(It beg, It end) -> typename std::remove_reference<decltype(*beg)>::type {
        return *beg;
    }

    template <typename T> int compare(const T&, const T&);
} // namespace template2

