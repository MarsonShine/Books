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

//
function loggingIdentity<T>(arg: Array<T>): Array<T> {
    console.log(arg.length);
    return arg;
}
//泛型类型
function identity<T>(arg: T): T {
    return arg;
}
let myIdentity: <T>(arg: T) => T = identity;
//对象字面量
let myIdentity2: { <T>(arg: T): T } = identity;

interface GenericIdentityFunc<T> {
    (arg: T): T;
}

let myIdentity3: GenericIdentityFunc<number> = identity;

class GenericNumber<T>{
    zeroValue: T;
    add: (x: T, y: T) => T;
}

let myGenericNumber = new GenericNumber<number>();

myGenericNumber.zeroValue = 26;
myGenericNumber.add = (x, y) => x + y;
//在泛型里使用类类型
function create<T>(c: { new(): T; }): T {
    return new c();
}


//使用原型属性推断并约束构造函数与类实例的关系。
class BeeKeeper {
    hasMask: boolean;
}

class ZooKeeper {
    nametag: string;
}

class Animal_D {
    numLegs: number;
}

class Bee extends Animal_D {
    keeper: BeeKeeper;
}

class Lion extends Animal_D {
    keeper: ZooKeeper;
}

function createInstance<A extends Animal_D>(c: new () => A): A {
    return new c();
}

createInstance(Lion).keeper.nametag;
createInstance(Bee).keeper.hasMask;