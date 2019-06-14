//类型断言不是类型转换，断言成一个联合类型中不存在的类型是不允许的
//格式  <Type>值 or 值 as Type
// function getLength(something: string | number): number {
//     return something.length;
// }//报错，联合类型参数必须要是多个类型的公共属性，length在number中是不存在
//可以用类型断言方法转换
function getLength1(something) {
    if (something.length) {
        return something.length;
    }
    else
        return something.toString().length;
}
