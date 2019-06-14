var mySearchFunc;
mySearchFunc = function (source, substring) {
    return (source + substring).length == 0;
};
//参数默认值
//给定默认值，会自动变为可选参数
function buildName(firstName, lastName) {
    if (lastName === void 0) { lastName = 'shine'; }
    return firstName + ' ' + lastName;
}
//剩余参数 rest parameters
//es6
function push(array) {
    var items = [];
    for (var _i = 1; _i < arguments.length; _i++) {
        items[_i - 1] = arguments[_i];
    }
    items.forEach(function (item) { return array.push(item); });
}
var array = [];
push(array, 1, 2, 3, 4);
//等价于
function pushEquivalence(array) {
    var items = [];
    for (var _i = 1; _i < arguments.length; _i++) {
        items[_i - 1] = arguments[_i];
    }
    items.forEach(function (item) { return array.push(item); });
}
pushEquivalence(array, 5, 6, 7, 8);
function reverse(x) {
    if (typeof x === 'number') {
        return Number(x.toString().split('').reverse().join(''));
    }
    else if (typeof x === 'string') {
        return x.split('').reverse().join('');
    }
}
