#include <vector>
#include <string>
#include <iostream>
#include <fstream>
#include <memory>
#include <map>
#include <set>

using std::string; using std::vector; 
using std::cout; using std::ifstream; using std::ostream; using std::istream;
using std::map; using std::set;
using std::shared_ptr;

class QueryResult;
class TextQuery {
public:
    typedef string::size_type LineNumber;
    TextQuery(ifstream& file);
    QueryResult query(const string&) const;
private:
    shared_ptr<vector<string>> content;
    map<string, shared_ptr<set<LineNumber>>> wordInLineMap;
};

class QueryResult {
public:
    QueryResult(const string &s, shared_ptr<std::set<TextQuery::LineNumber>> set, shared_ptr<vector<string>> v) : word(s), nos(set), input(v) { }
    std::set<TextQuery::LineNumber>::iterator begin() { return nos->begin(); }
    std::set<TextQuery::LineNumber>::iterator end() { return nos->end(); }
    // const StrBlob& get_file() const{ return file; }
private:
    string word;
    shared_ptr<std::set<TextQuery::LineNumber>> nos;
    shared_ptr<vector<string>> input;
};

std::ostream& print(std::ostream &, const QueryResult&);