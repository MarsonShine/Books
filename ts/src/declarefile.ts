//通常我们会把声明语句放到一个单独的文件（jQuery.d.ts）中
//声明文件必需以 .d.ts 为后缀
//ts会解析所有带*.ts文件，也包括.d.ts结尾文件
//如果解析不了，查看tsconfig文件有没有把对应的目录包含进去
JQuery('#id');

let Cat = new Animal('niuniu');
//declare enum
let directions = [Directions.Up, Directions.Down, Directions.Left, Directions.Right];
jQuery.ajax('api/version');