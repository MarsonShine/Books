//用接口定义函数契约
interface SearchFunc {
    (source: string, substring: string): boolean;
}
let mySearchFunc: SearchFunc;
mySearchFunc = function (source, substring) {
    return (source + substring).length == 0;
}

//参数默认值
//给定默认值，会自动变为可选参数
function buildName(firstName: string, lastName: string = 'shine') {
    return firstName + ' ' + lastName;
}

//剩余参数 rest parameters
//es6
function push(array, ...items) {
    items.forEach(item => array.push(item));
}
let array = [];
push(array, 1, 2, 3, 4);
//等价于
function pushEquivalence(array: any[], ...items: any[]) {
    items.forEach(item => array.push(item));
}
pushEquivalence(array, 5, 6, 7, 8);

//overload
// function reverse(x: number | string): number | string {
//     if (typeof x === 'number')
//         return Number(x.toString().split('').reverse().join(''));
//     else if (typeof x === 'string')
//         return x.split('').reverse().join('');
// }
//以上number|string 二选一，但是还是存在输入number返回string的可能，要杜绝这种情况
//用一下函数重载
function reverse(x: number): number;
function reverse(x: string): string;
function reverse(x: number | string): number | string {
    if (typeof x === 'number') {
        return Number(x.toString().split('').reverse().join(''));
    } else if (typeof x === 'string') {
        return x.split('').reverse().join('');
    }
}