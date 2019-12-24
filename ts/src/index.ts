import { name, getName, Animal, Directions, Options } from 'foo';
import poo = require("poo");
import aoo = require('aoo');

console.log(name);
let myName = getName();
let cat = new Animal('Tom');
let directions = [Directions.Up, Directions.Down, Directions.Left, Directions.Right];
let options: Options = {
    data: {
        name: 'foo'
    }
};
//export as namespace 的变量可以直接作为全局变量饮用
aoo();
console.log(aoo.bar);
//declare global
'bar'.prependHello();