enum Days { Sun, Mon, Tue, Wed, Thu, Fri, Sat };
console.log(Days["Sun"] === 0); // true
console.log(Days["Mon"] === 1); // true
console.log(Days["Tue"] === 2); // true
console.log(Days["Sat"] === 6); // true

console.log(Days[0] === "Sun"); // true
console.log(Days[1] === "Mon"); // true
console.log(Days[2] === "Tue"); // true
console.log(Days[6] === "Sat"); // true

//等价于
var Dayseq;
(function (Dayseq) {
    Dayseq[Dayseq['Sun'] = 0] = "Sun";
    Dayseq[Dayseq["Mon"] = 1] = "Mon";
    Dayseq[Dayseq["Tue"] = 2] = "Tue";
    Dayseq[Dayseq["Wed"] = 3] = "Wed";
    Dayseq[Dayseq["Thu"] = 4] = "Thu";
    Dayseq[Dayseq["Fri"] = 5] = "Fri";
    Dayseq[Dayseq["Sat"] = 6] = "Sat";
})(Dayseq || (Dayseq = {}));