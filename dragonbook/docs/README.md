# 编译原理

一个编译器前端的模型如下图：

![](asserts/1.png)

## 词法分析

词法分析是编译的第一阶段。**词法分析器**的主要任务是读入源程序的输入字符、将它们组成**词素**，生成并输出一个**词法单元**序列，每个词法单元对应于一个词素。这个词法单元序列被输出到**语法分析器**进行语法分析。

词法分析器通常还要和**符号表**进行交互。当词法分析器发现了 一个标识符的词素时，它要将这个词素添加到符号表中。在某些情况下，词法分析器会从符号表中读取有关标识符种类的信息，以确定向语法分析器传送哪个词法单元。

![](asserts/2.png)

再词法分析过程中，我们需要知道以下三个术语：

- **词法单元（Tokens）**：由一个词法单元名和一个可选的属性值组成；主要的词法单元包括标识符、字符串、格式说明符、标点符号等。
- **模式（Patterns）**：是描述词法单元的规则或正则表达式。
- **词素（Lexemes）**：是源程序中实际匹配模式的具体示例。

以具体的例子进一步说明：

请分别标出 `printf("Total= % d\n",score)` 中的词法单元、模式和词素。

| 词素 (Lexeme)     | 词法单元 (Token)             | 模式 (Pattern)           |
| ----------------- | ---------------------------- | ------------------------ |
| `print`           | 标识符 (Identifier)          | `[a-zA-Z_][a-zA-Z0-9_]*` |
| `(`               | 左圆括号 (Left Parenthesis)  | `\(`                     |
| `"Total = %d \n"` | 字符串 (String Literal)      | `"[^"]*"`                |
| `,`               | 逗号 (Comma)                 | `,`                      |
| `score`           | 标识符 (Identifier)          | `[a-zA-Z_][a-zA-Z0-9_]*` |
| `)`               | 右圆括号 (Right Parenthesis) | `\)`                     |

## LEX 程序

词法分析器生成工具 Lex，也叫 Flex。它支持使用正则表达式来描述各个词法单元的模式，由此给出一个词法分析器的规约。Lex 工具的输入表示方法称为 Lex 语言(Lex language)，而工具本身则称为 Lex 编译器(Lex compiler) 。在它的核心部分,  Lex 编译器将输入的模式转换成一个状态转换图，并生成相应的实现代码，

一个典型的 Lex 程序结构如下：

```
%{
/* C 代码 */
%}

/* 定义部分 */
%%

/* 规则部分 */
%%

/* 用户代码部分 */
```

#### 1. 定义部分（申明部分）

位于 `%{` 和 `%}` 之间，用于包含需要的 C 代码或宏定义。这些代码会被直接包含在生成的词法分析器的 C 文件中，通常用于包含头文件、定义常量和声明全局变量。

```
%{
#include <stdio.h>
%}

```

#### 2. 规则部分

规则部分包含一系列模式和相应的动作。每一行定义一个规则，模式部分是正则表达式，用于匹配输入文本中的模式；动作部分是 C 代码，当模式匹配时执行相应的动作。

```
[ \t\n]+    { printf(" "); }
[^ \t\n]+   { printf("%s", yytext); }
```

#### 3. 用户代码部分（辅助函数）

用户代码部分包含主函数以及其他用户定义的函数。这部分代码在规则部分之后，通常用于实现程序的入口和其他必要的逻辑。

一个完整的 LEX 程序示例：

```
%{
	/*  definitions of manifest constants
	LT, LE, EQ, NE, GT, GE,
	IF, THEN, ELSE, ID, NUMBER, RELOP */
%}

/* regular definitions */
delim [ \t\n]
ws {delim}+
letter [A-Za-z]
digit [0-9]
id {letter}({letter}|{digit})*
number {digit}+(\.{digit}+)?(E[+-]?{digit}+)?

%%

{ws}		{/*  no action and no return */}
if			{return(IF);}
then		{return(THEN);}
else 		{return(ELSE);}
{id}		{yylval = (int) installlDO; return(ID);}
{number}	{yylval = (int) installNumO; return(NUMBER);}
"<"			{yylval = LT; return(RELOP);}
"<="		{yylval = LE; return(RELOP);}
"="			{yylval = EQ; return(RELOP);}
"<>"		{yylval = NE; return(RELOP);}
">"			{yylval = GT; return(RELOP);}
">="		{yylval = GE; return(RELOP);}

%%

int installlD() {/* function to install the lexeme, whose first character is pointed to by yytext, and whose length is yyleng, into the symbol table and return a pointer thereto */
}

int installNumO {/* similar to installlD, but puts numer-ictal constants into a separate table */
}
```

