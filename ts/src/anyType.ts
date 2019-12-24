//any 表示是任意类型
let value: any = "marson shine";
value = 25;
//可以在any上调用属性和方法（这些都允许调用不存在的属性和方法）
let anything: any = "marson";
anything.reName('summerzhu');
anything.Name = 'summerzhu';
//未声明任何类型的变量默认为any任意类型
let v;
v = "marson shine";
v = 25;
//等价于
let v2: any;
v2 = "marson shine";
v2 = 25;