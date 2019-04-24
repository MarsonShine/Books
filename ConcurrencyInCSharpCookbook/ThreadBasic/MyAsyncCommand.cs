using System.Threading;
using System.Threading.Tasks;

namespace ThreadBasic
{
    //2.9 处理async avoid方法的异常
    public class MyAsyncCommand : ICommand
    {
        async void ICommand.Execute(object parameter)
        {
            await Execute(parameter);
        }

        public async Task Execute(object parameter){
            //这里实现异步代码
            await Task.CompletedTask;
        }
    }
}