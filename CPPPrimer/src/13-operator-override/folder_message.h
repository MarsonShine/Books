#include <string>
#include <set>

class Folder;
class Message {
public:
    // 隐式初始化空消息
    explicit Message(const std::string& msg = "")
        : content(msg) { }
    Message(const std::string&); // 拷贝构造函数
    Message& operator=(const Message&); // 拷贝复制运算符
    ~Message();
    void save(Folder&);
    void remove(Folder&);
    void swap(Message& lm, Message &rm) {
        using std::swap;
        // 删消息
        for (auto f : lm.folders) {
            f->removeMessage(&lm);
        }
        for (auto f : rm.folders)
            f->removeMessage(&rm);
        swap(lm.folders, rm.folders);
        swap(lm.content, rm.content);
        // 添加消息
        for (auto f : lm.folders) {
            f->addMessage(&lm);
        }
        for (auto f : rm.folders) {
            f->addMessage(&rm);
        }
    }
private:
    std::string content;
    std::set<Folder*> folders;
    void add_to_folder(const Message&);
    void remove_from_folders();
};

void Message::save(Folder &f) 
{
    folders.insert(&f);
    f.addMessage(this);
}
void Message::remove(Folder &f) {
    folders.erase(&f);
    f.removeMessage(this);
}
// 将本消息加到指定的文件夹中
void Message::add_to_folder(const Message &message)
{
    for (auto f : message.folders)
    {
        f->addMessage(this);
    }
}
void Message::remove_from_folders()
{
    for (auto f : folders)
    {
        f->removeMessage(this);
    }
}
// 构造函数
Message::Message(const Message &m): content(m.content), folders(m.folders)
{
    add_to_folder(m);
}
// 拷贝复制运算符

Message::~Message()
{
    remove_from_folders();
}
Message& Message::operator=(const Message &m)
{
    // 删除原来内容
    remove_from_folders();
    content = m.content;
    folders = m.folders;
    add_to_folder(m);
    return *this;
}

class Folder {
    friend void swap(Folder &, Folder &);
    friend class Message;
public:
    Folder() = default;
    Folder(const Folder&);
    Folder& operator=(const Folder&);
    ~Folder();

    void print();
private:
    std::set<Message*> msgs;

    void add_to_message(const Folder&);
    void remove_from_message();

    void addMessage(Message *m) { msgs.insert(m); }
    void removeMessage(Message *m) { msgs.erase(m); }
};

void swap(Folder&, Folder&);