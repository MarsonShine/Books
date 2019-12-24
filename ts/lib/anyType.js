//any 表示是任意类型
var value = "marson shine";
value = 25;
//可以在any上调用属性和方法（这些都允许调用不存在的属性和方法）
var anything = "marson";
anything.reName('summerzhu');
anything.Name = 'summerzhu';
//未声明任何类型的变量默认为any任意类型
var v;
v = "marson shine";
v = 25;
//等价于
var v2;
v2 = "marson shine";
v2 = 25;
