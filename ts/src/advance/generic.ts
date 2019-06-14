function createArray<T>(length: number, value: T): Array<T> {
    let result: T[] = [];
    for (let i = 0; i < length; i++) {
        result[i] = value;
    }
    return result;
}

createArray(3, 'marson');
createArray<string>(3, 'marson');
//多个类型参数
function swap<T, U>(tuple: [T, U]): [U, T] {
    return [tuple[1], tuple[0]];
}
//泛型约束
interface Lengthwise {
    length: number;
}
function logginIdentity<T extends Lengthwise>(arg: T): T {
    console.log(arg.length);
    return arg;
}
//多个参数之间的互相约束
function copyFields<T extends U, U>(target: T, source: U): T {
    for (let id in source) {
        target[id] = (<T>source)[id];
    }
    return target;
}
let x = { a: 1, b: 2, c: 3, d: 4 };
copyFields(x, { b: 10, d: 20 });
//泛型接口

interface CreateArrayFunc<T> {
    (length: number, value: T): T[];
}
let createArray2: CreateArrayFunc<string>;
createArray2 = function (length: number, value: string): string[] {
    let result: string[] = [];
    for (let i = 0; i < length; i++) {
        result[i] = value;
    }
    return result;
}

//类型约束为某个对象的属性
function getProperty<T, K extends keyof T>(obj: T, key: K): any {
    return obj[key];
}
let x = { a: 1, b: 2, c: 3, d: 4 };

getProperty(x, "a"); // okay
// getProperty(x, "m"); // error: Argument of type 'm' isn't assignable to 'a' | 'b' | 'c' | 'd';