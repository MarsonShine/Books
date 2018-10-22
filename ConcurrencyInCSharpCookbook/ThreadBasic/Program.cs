using System;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace ThreadBasic {
    //如何在async await中处理异常
    class Program {
        static int Main (string[] args) {
            try {
                return AsyncContext.Run (() => MainAsync (args));
            } catch (Exception ex) {
                Console.Error.WriteLine (ex);
                return -1;
            }
        }

        static async Task<int> MainAsync(string[] args){
            //...
            return await Task.FromResult(1);
        } 
    }
}