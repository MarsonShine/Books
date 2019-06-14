interface SearchFunc {
    (source: string, substring: string): boolean;
}
declare let mySearchFunc: SearchFunc;
declare function buildName(firstName: string, lastName?: string): string;
declare function push(array: any, ...items: any[]): void;
declare let array: any[];
declare function pushEquivalence(array: any[], ...items: any[]): void;
declare function reverse(x: number): number;
declare function reverse(x: string): string;
