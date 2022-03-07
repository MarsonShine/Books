namespace exam16
{
    template <typename Iterator, typename TVal>
    Iterator find(Iterator begin,Iterator end, TVal const& value) {
        for (; begin != end && *begin != value; ++begin)
        return begin;
    } 
} // namespace exam16


