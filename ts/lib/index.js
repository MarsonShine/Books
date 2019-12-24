"use strict";
exports.__esModule = true;
var foo_1 = require("foo");
var aoo = require("aoo");
console.log(foo_1.name);
var myName = foo_1.getName();
var cat = new foo_1.Animal('Tom');
var directions = [foo_1.Directions.Up, foo_1.Directions.Down, foo_1.Directions.Left, foo_1.Directions.Right];
var options = {
    data: {
        name: 'foo'
    }
};
//export as namespace 的变量可以直接作为全局变量饮用
aoo();
console.log(aoo.bar);
//declare global
'bar'.prependHello();
