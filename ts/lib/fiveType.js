//javascript 5种类型
//boolean Numeric string null undefined
//ES6 中海油 Symbal 类型
//boolean
var isDone = false;
var createdByNewBoolean = new Boolean(1);
//number
var decLiteral = 6;
var hexLiteral = 0xf00d;
// ES6 中的二进制表示法
var binaryLiteral = 10;
// ES6 中的八进制表示法
var octalLiteral = 484;
var notANumber = NaN;
var infinityNumber = Infinity;
//string
var myName = 'Tom';
var myAge = 25;
// 模板字符串
var sentence = "Hello, my name is " + myName + ".\nI'll be " + (myAge + 1) + " years old next month.";
// NULL 空值
//typescript 没有空值的概念，但是可以用 Void 来没有返回值的函数
function NoReturn() {
    alert('marson shine');
}
//Null Undefined 
// undefined 只能赋值为 undefined
// null 也只能赋值为 null
// 并且 null，undefined 是所有基本类型的子类，即 null，undefined 能复制给所有的基本类型
