// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
// 错误异常处理的陷阱，exception catch when 执行顺序问题
// https://x.com/ericlippert/status/1717203249898619248?s=12
try
{
    HighTrust();
}
catch (Exception) when (PayLoad())
{
    Console.WriteLine("High trust runs but not as admin");  
}

bool PayLoad()
{
    Console.WriteLine("Low trust runs as admin");
    return true;
}

void HighTrust()
{
    try
    {
        ImpresonateAdmin();
        DoSomething();
    }
    finally
    {
        RevertAdmin();
    }
}

void DoSomething()
{
    Console.WriteLine("High trust code runs as admin");
    Console.WriteLine("OH NO");
    throw new Exception();
}

void ImpresonateAdmin()
{
    Console.WriteLine("Impresonating admin");
}

void RevertAdmin()
{
    Console.WriteLine("Reverting admin");
}