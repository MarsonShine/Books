// export const name: string;
// export function getName(): string;
// export class Animal {
//     constructor(name: string);
//     sayHi(): string;
// }
// export enum Directions {
//     Up,
//     Down,
//     Left,
//     Right
// }
// export interface Options {
//     data: any;
// }
//以上等价于
declare const name: string;
declare function getName(): string;
declare class Animal {
    constructor(name: string);
    sayHi(): string;
}
declare enum Directions {
    Up,
    Down,
    Left,
    Right
}
interface Options {
    data: any;
}

export { name, getName, Animal, Directions, Options };