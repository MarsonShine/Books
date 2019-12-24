class DAnimal {
    public name;
    public constructor(name) {
        this.name = name;
    }
}

class DCat extends DAnimal {
    constructor(name) {
        super(name);
        //DAnimal's property level is public/protected
        //private is not accessible;
        console.log(this.name);
    }
}

//抽象类
//不允许实例化，其抽象方法必须要在其继承的子类的实现
abstract class AnimalBase {
    public name;
    public constructor(name) {
        this.name = name;
    }
    public abstract sayHello();
}

class DDAnimal {
    name: string;
    constructor(name: string) {
        this.name = name;
    }
    sayHello(): string {
        return `my name is ` + this.name;
    }
}

//类实现接口
interface Alarm {
    alert();
}
class Door { }
class securityDoor extends Door implements Alarm {
    alert() {
        console.log('SecurityDoor Alarm');
    }
}
class DCar implements Alarm {
    alert() {
        console.log('Car Alarm');
    }

}
//多个接口实现
interface Alarm {
    alert();
}

interface Light {
    lightOn();
    lightOff();
}

class Car implements Alarm, Light {
    alert() {
        console.log('Car alert');
    }
    lightOn() {
        console.log('Car light on');
    }
    lightOff() {
        console.log('Car light off');
    }
}

//接口继承类
class Point {
    x: number;
    y: number;
}
interface Point3D extends Point {
    z: number;
}
let point3D: Point3D = { x: 1, y: 2, z: 3 };