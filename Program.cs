// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using CSL.BooleanExpression;
using System.Diagnostics;

async Task<dynamic> f(int argc, dynamic[] args)
{
    return args[0] + args[1];
}

async Task<dynamic> SUM(int argc, dynamic[] args)
{
    int sum = 0;
    for (var i = 0; i < argc; ++i)
        sum += args[i];
    return sum;
}

async Task<dynamic> CONCAT(int argc, dynamic[] args)
{
    string sum = "";
    for (var i = 0; i < argc; ++i)
        sum += args[i];
    return sum;
}

async Task<dynamic> STR_LENGTH(int argc, dynamic[] args)
{
    string str = args[0];
    return str.Length;
}


var bExpr = new ExecutableExpression("STR_LENGTH(CONCAT(\"a\", \"b\")) <= STR_LENGTH(\"a\")");

var functions = bExpr.GetFunctions();
var variables = bExpr.GetVariables();

Random rnd = new Random();
bExpr.SetVariable("x", rnd.Next(-3123, 3123));
bExpr.SetVariable("z", rnd.Next(-3123, 3123));
bExpr.SetVariable("n", rnd.Next(-3123, 3123));

bExpr.SetFunction("f", f);
bExpr.SetFunction("SUM", SUM);
bExpr.SetFunction("CONCAT", CONCAT);
bExpr.SetFunction("STR_LENGTH", STR_LENGTH);




await bExpr.ExecuteAsync();
GC.Collect();
Stopwatch sw = Stopwatch.StartNew();
//for (int i = 0; i < 10000000; ++i)
//{
//    bExpr.SetVariable("x", rnd.Next(-3123, 3123));
//    bExpr.SetVariable("z", rnd.Next(-3123, 3123));
//    bExpr.SetVariable("n", rnd.Next(-3123, 3123));
//    await bExpr.ExecuteAsync();
//}

var result = await bExpr.ExecuteAsync();

sw.Stop();
Console.WriteLine(result);
Console.WriteLine((sw.ElapsedMilliseconds).ToString() + "ms");

//sw.Restart();
//sw.Start();
//var argsS = new dynamic[10];
//argsS[0] = (rnd.Next(-3123, 3123));
//argsS[1] = (2);
//argsS[2] = (rnd.Next(-3123, 3123));
//argsS[3] = (4);
//argsS[4] = (5);
//argsS[5] = (6);
//argsS[6] = (7);
//argsS[7] = (8);
//argsS[8] = (rnd.Next(-3123, 3123));
//argsS[9] = (10);

//for (int i = 0; i < 10000000; ++i)
//{
//    argsS[0] = rnd.Next(-3123, 3123);
//    argsS[2] = rnd.Next(-3123, 3123);
//    argsS[8] = rnd.Next(-3123, 3123);

//    await SUM(10, argsS.ToArray());
//}

//sw.Stop();
//Console.WriteLine((sw.ElapsedMilliseconds).ToString());