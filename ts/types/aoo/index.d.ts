export as namespace aoo;    //export as namespace 可以直接设为变量为全局变量直接饮用
export = aoo;

declare function aoo(): string;
declare namespace aoo {
    const bar: number;
}