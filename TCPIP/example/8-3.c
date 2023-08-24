#include<sys/socket.h>
#include<netinet/in.h>
#include<arpa/inet.h>
#include<assert.h>
#include<stdio.h>
#include<stdlib.h>
#include<unistd.h>
#include<errno.h>
#include<string.h>
#include<fcntl.h>

// HTTP请求的读取和分析
#define BUFFER_SIZE 4096/*读缓冲区大小*/
/*主状态机的两种可能状态，分别表示：当前正在分析请求行，当前正在分析头部字段*/
enum CHECK_STATE{CHECK_STATE_REQUESTLINE=0,CHECK_STATE_HEADER};
/*从状态机的三种可能状态，即行的读取状态，分别表示：读取到一个完整的行、行出错和行数据尚且不完整*/
enum LINE_STATUS{LINE_OK=0,LINE_BAD,LINE_OPEN};
/*服务器处理HTTP请求的结果：NO_REQUEST表示请求不完整，需要继续读取客户数据；
GET_REQUEST 表示获得了一个完整的客户请求；
BAD_REQUEST 表示客户请求有语法错误；
FORBIDDEN_REQUEST 表示客户对资源没有足够的访问权限；
INTERNAL_ERROR 表示服务器内部错误；
CLOSED_CONNECTION 表示客户端已经关闭连接了*/
enum HTTP_CODE{NO_REQUEST,GET_REQUEST,BAD_REQUEST,FORBIDDEN_REQUEST,INTERNAL_ERROR,CLOSED_CONNECTION};
/*为了简化问题，我们没有给客户端发送一个完整的HTTP应答报文，而只是根据服务器的处理结果发送如下成功或失败信息*/
static const char *szret[]={"I get a correct result\n","Something wrong\n"};
/*从状态机，用于解析出一行内容*/
LINE_STATUS parse_line(char *buffer,int &checked_index,int &read_index)
{
    char temp;
    /*checked_index指向buffer（应用程序的读缓冲区）中当前正在分析的字节，
    read_index指向buffer中客户数据的尾部的下一字节。
    buffer中第0～checked_index字节都已分析完毕，第checked_index～(read_index-1)字节由下面的循环挨个分析*/
    for(;checked_index<read_index;++checked_index)
    {
        /*获得当前要分析的字节*/
        temp=buffer[checked_index];
        /*如果当前的字节是“\r”，即回车符，则说明可能读取到一个完整的行*/
        if(temp=='\r')
        {
            /*如果“\r”字符碰巧是目前buffer中的最后一个已经被读入的客户数据，
            那么这次分析没有读取到一个完整的行，返回LINE_OPEN以表示还需要继续读取客户数据才能进一步分析*/
            if((checked_index+1)==read_index)
            {
                return LINE_OPEN;
            }
            /*如果下一个字符是“\n”，则说明我们成功读取到一个完整的行*/
            else if(buffer[checked_index+1]=='\n')
            {
                buffer[checked_index++]='\0';
                buffer[checked_index++]='\0';
                return LINE_OK;
            }
            /*否则的话，说明客户发送的HTTP请求存在语法问题*/
            return LINE_BAD;
        }
        /*如果当前的字节是“\n”，即换行符，则也说明可能读取到一个完整的行*/
        else if(temp=='\n')
        {
            if((checked_index>1)&&buffer[checked_index-1]=='\r')
            {
                buffer[checked_index-1]='\0';
                buffer[checked_index++]='\0';
                return LINE_OK;
            }
            return LINE_BAD;
        }
    }
    /*如果所有内容都分析完毕也没遇到“\r”字符，则返回LINE_OPEN，表示还需要继续读取客户数据才能进一步分析*/
    return LINE_OPEN;
}

/*分析请求行*/
HTTP_CODE parse_requestline(char *temp,CHECK_STATE &checkstate)
{
    char *url=strpbrk(temp,"\t");
    /*如果请求行中没有空白字符或“\t”字符，则HTTP请求必有问题*/
    if(!url)
    {
        return BAD_REQUEST;
    }
    *url++='\0';
    char *method=temp;
    if(strcasecmp(method,"GET")==0)/*仅支持GET方法*/
    {
        printf("The request method is GET\n");
    }
    else
    {
        return BAD_REQUEST;
    }
    url+=strspn(url,"\t");
    char *version=strpbrk(url,"\t");
    if(!version)
    {
    return BAD_REQUEST;
    }
    *version++='\0';
    version+=strspn(version,"\t");
    /*仅支持HTTP/1.1*/
    if(strcasecmp(version,"HTTP/1.1")!=0)
    {
        return BAD_REQUEST;
    }

    /*检查URL是否合法*/
    if(strncasecmp(url,"http://",7)==0)
    {
        url+=7;
        url=strchr(url,'/');
    }
    if(!url||url[0]!='/')
    {
        return BAD_REQUEST;
    }
        printf("The request URL is:%s\n",url);
        /*HTTP请求行处理完毕，状态转移到头部字段的分析*/
        checkstate=CHECK_STATE_HEADER;
        return NO_REQUEST;
}

/*分析头部字段*/
HTTP_CODE parse_headers(char*temp)
{
    // TODO
}
