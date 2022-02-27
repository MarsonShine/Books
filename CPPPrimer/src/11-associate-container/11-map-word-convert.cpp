#include <map>
#include <string>
#include <fstream> 
#include <iostream>
#include <sstream>

using std::map; using std::string; using std::pair; using std::ifstream;

const string& transform(const string &s, const map<string, string> &m)
{
    auto map_it = m.find(s);
    if (map_it != m.cend())
    {
        return map_it->second;
    }
    else
        return s;
    
}

map<string, string> buildMap(ifstream &map_file)
{
    map<string, string> trans_map;
    string key;
    string value;
    while (map_file >> key && getline(map_file, value)) // 如果是以其它分割符读取单词，则需要传第三个参数
    {
        if (value.size() > 1) 
            trans_map[key] = value.substr(1);
        else
            throw std::runtime_error("no rule for " + key);
    }
    return trans_map;
}

void word_transform(ifstream &map_file, ifstream &input)
{
    auto trans_map = buildMap(map_file);
    string text;
    while (getline(input, text))
    {
        std::istringstream iss(text); // 读每个单词
        string word;
        bool firstword = true;
        while (iss >> word)
        {
            if (firstword)
                firstword = false;
            else
                std::cout << " ";
            // 转换
            std::cout << transform(word, trans_map); // 打印输出
        }
        std::cout << std::endl;
    }
    
}

int main()
{
    std::ifstream ifs("./tmp/rule.txt");
    std::ifstream ifss("./tmp/content.txt");
    word_transform(ifs, ifss);
}