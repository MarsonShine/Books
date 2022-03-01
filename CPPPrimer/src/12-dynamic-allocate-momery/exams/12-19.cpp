#include "weakstrblob.h"

WeakStrBlob StrBlob::begin()
{
    return WeakStrBlob(*this);
}

WeakStrBlob StrBlob::end()
{
    return WeakStrBlob(*this, data->size());
}