//ES6 class定义
class CAnimal {
    constructor(name) {
        this.name = name;
    }
    sayHello() {
        return `my name is ${this.name}`;
    }
}
//继承
class Cat extends CAnimal {
    constructor(name) {
        super(name);//call parent's constructor
        console.log(this.name);
    }
    sayHello() {
        return `meow, ` + super.sayHello();
    }
}
//存取器，getter，setter
class BAnimal {
    constructor(name) {
        this.name = name;
    }

    set name(value) {
        this.name = value;
    }

    get name() {
        return this.name;
    }
}
//static method
class CAnimal {
    static isAnimal(a) {
        return a instanceof CAnimal;
    }
}