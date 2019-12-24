declare function JQuery(selector: string): any;  //只能定义类型不能具体实现

// 当然，jQuery 的声明文件不需要我们定义了，社区已经帮我们定义好了：jQuery in DefinitelyTyped。
// 我们可以直接下载下来使用，但是更推荐的是使用 @types 统一管理第三方库的声明文件。
// @types 的使用方式很简单，直接用 npm 安装对应的声明模块即可，以 jQuery 举例：
//npm install @types/jquery --save-dev
//declare namespace 表示这个全局变量是个对象，它包含了很多属性
declare namespace JQuery {
    function ajax(url: string, settings?: any): any;
    const version: number;
    class Event {
        blur(eventType: EventType): void;
    }
    enum EventType {
        CustomClick
    }
    namespace fn {
        function extend(object: any): void;
    }
}
//declarefile.ts 中就可以引用这个ajax属性