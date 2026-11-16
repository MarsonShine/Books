string selection = args.Length == 0 ? "all" : args[0].ToLowerInvariant();
if (selection is not ("all" or "language" or "runtime" or "libraries" or "extra"))
{
    Console.Error.WriteLine("用法: dotnet run -- [all|language|runtime|libraries|extra]");
    return 1;
}
if (selection is "all" or "language") LanguageDemos.Run();
if (selection is "all" or "runtime") await RuntimeDemos.RunAsync();
if (selection is "all" or "libraries") await LibraryDemos.RunAsync();
if (selection is "all" or "extra") await ExtraLibraryDemos.RunAsync();
return 0;
