//javascript 5种类型
//boolean Numeric string null undefined
//ES6 中海油 Symbal 类型

//boolean
let isDone: boolean = false;
let createdByNewBoolean: Boolean = new Boolean(1);

//number
let decLiteral: number = 6;
let hexLiteral: number = 0xf00d;
// ES6 中的二进制表示法
let binaryLiteral: number = 0b1010;
// ES6 中的八进制表示法
let octalLiteral: number = 0o744;
let notANumber: number = NaN;
let infinityNumber: number = Infinity;

//string
let myName: string = 'Tom';
let myAge: number = 25;

// 模板字符串
let sentence: string = `Hello, my name is ${myName}.
I'll be ${myAge + 1} years old next month.`;

// NULL 空值
//typescript 没有空值的概念，但是可以用 Void 来没有返回值的函数
function NoReturn(): void {
    alert('marson shine');
}

//Null Undefined 
// undefined 只能赋值为 undefined
// null 也只能赋值为 null
// 并且 null，undefined 是所有基本类型的子类，即 null，undefined 能复制给所有的基本类型