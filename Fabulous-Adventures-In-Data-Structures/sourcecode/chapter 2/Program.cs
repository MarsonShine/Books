// See https://aka.ms/new-console-template for more information
using chapter_2;
using Covariance;
using static System.Console;

WriteLine("Hello, World!");

var s1 = chapter_2.ImStack<int>.Empty;
var s2 = s1.Push(10);
var s3 = s2.Push(20);
var s4 = s2.Push(30); // #A
var s5 = s4.Pop();
var s6 = s5.Pop();
WriteLine(s1.Bracket());
WriteLine(s2.Bracket());
WriteLine(s3.Bracket());
WriteLine(s4.Bracket());
WriteLine(s5.Bracket());
WriteLine(s6.Bracket());
ReadLine();

CovImmutableStack.SampleCode();
ImmutableQueue.SampleCode();