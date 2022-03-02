#include <sstream>
#include <algorithm>
#include "query_word.h"

TextQuery::TextQuery(std::ifstream &fileStream) : content(new vector<string>()) { 
    // 读取文件内容复制到content中
    LineNumber ln(0);
    for (string line; std::getline(fileStream, line); ++ln) {
        content->push_back(line);
        std::istringstream line_stream(line);
        // 读取每个单词，记录重复的次数与行号
        for (string text,word; line_stream >> text; word.clear()) {
            std::remove_copy_if(text.cbegin(), text.cend(), std::back_inserter(word), ispunct);
            auto &nos = wordInLineMap[word];
            if (!nos) nos.reset(new set<LineNumber>);
            nos->insert(ln);
        }
    }
}

QueryResult TextQuery::query(const string& s) const 
{
    auto found = wordInLineMap.find(s);
    auto nos = std::make_shared<set<LineNumber>>(new set<LineNumber>);
    if (found == wordInLineMap.end()) return QueryResult(s, nos, content);
    else return QueryResult(s, found->second, content);
}
