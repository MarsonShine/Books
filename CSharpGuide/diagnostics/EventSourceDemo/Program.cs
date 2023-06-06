// See https://aka.ms/new-console-template for more information

#define DiagnosticSource

using EventSourceDemo;
using EventSourceDemo.EventListeners;
using System.Diagnostics;

// EventListener
var listener = new ConsoleWriterEventListener();

#region ParallelActivityIDs
await ProcessWorkItem2("A");
static async Task Query(string query)
{
    ParallelActivityIdEventSource.Log.QueryStart(query);
    await Task.Delay(100); // pretend to send a query
    ParallelActivityIdEventSource.Log.DebugMessage("processing query");
    await Task.Delay(100); // pretend to do some work
    ParallelActivityIdEventSource.Log.QueryStop();
}

static async Task ProcessWorkItem2(string requestName)
{
    ParallelActivityIdEventSource.Log.WorkStart(requestName);
    Task query1 = Query("SELECT bowls");
    Task query2 = Query("SELECT spoons");
    await Task.WhenAll(query1, query2);
    ParallelActivityIdEventSource.Log.WorkStop();
}
#endregion

#if ActivityIDs
#region ActivityIDs
Task a = ProcessWorkItem("A");
Task b = ProcessWorkItem("B");
await Task.WhenAll(a, b);
static async Task ProcessWorkItem(string requestName)
{
    ActivityIdEventSource.Log.WorkStart(requestName);
    await HelperA();
    await HelperB();
    ActivityIdEventSource.Log.WorkStop();
}

static async Task HelperA()
{
    ActivityIdEventSource.Log.DebugMessage("HelperA");
    await Task.Delay(100); // pretend to do some work
}

static async Task HelperB()
{
    ActivityIdEventSource.Log.DebugMessage("HelperB");
    await Task.Delay(100); // pretend to do some work
}
#endregion
#endif

#if DiagnosticSource
DiagnosticSourceQuickStart.Start();
#endif
//Console.WriteLine("Hello, World!");
//CustomEventSource.Log.AppStarted("Hello, World!", 12);

//CustomEventSource.Log.DebugMessage("Got here!");
//CustomEventSource.Log.DebugMessage("finishing startup");
//CustomEventSource.Log.RequestStart(3);
//CustomEventSource.Log.RequestStop(3);

