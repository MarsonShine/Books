// See https://aka.ms/new-console-template for more information
using EventSourceDemo;
using EventSourceDemo.EventListeners;

// EventListener
var listener = new ConsoleWriterEventListener();

Console.WriteLine("Hello, World!");
CustomEventSource.Log.AppStarted("Hello, World!", 12);

CustomEventSource.Log.DebugMessage("Got here!");
CustomEventSource.Log.DebugMessage("finishing startup");
CustomEventSource.Log.RequestStart(3);
CustomEventSource.Log.RequestStop(3);

