int main(int argc, char const *argv[])
{
    // 溢出
    // 超出类型所表示的最大/小范围
    short svalue = 32767; ++ svalue;    // 溢出
    unsigned uivalue = 0; -- uivalue;
    unsigned short usvalue = 65536; ++ usvalue;
    return 0;
}
