#!/usr/bin/env dotnet
#:property PublishAot=false
#:include Greeting.cs

Console.WriteLine(Greeting.For(Environment.GetEnvironmentVariable("DOTNET11_DEMO_NAME") ?? "读者"));
