using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// https://devblogs.microsoft.com/premier-developer/dissecting-the-async-methods-in-c/
/// </summary>
namespace CSharpGuide.async
{
    public class StockPrices
    {
        [AllowNull]
        private Dictionary<string, decimal> _stockPrices;
        public async Task<decimal> GetStockPriceForAsync(string companyId)
        {
            await InitializeMapIfNeededAsync();
            _stockPrices.TryGetValue(companyId, out var result);
            return result;
        }

        private async Task InitializeMapIfNeededAsync()
        {
            if (_stockPrices != null)
                return;

            await Task.Delay(42);
            // Getting the stock prices from the external source and cache in memory.
            _stockPrices = new Dictionary<string, decimal> { { "MSFT", 42 } };
        }


        class GetStockPriceForAsync_StateMachine
        {
            enum State { Start,Step1,}
            private readonly StockPrices @this;
            private readonly string _companyId;
            [AllowNull]
            private readonly TaskCompletionSource<decimal> _tcs;
            [AllowNull]
            private Task _initializeMapIfNeededTask;
            private State _state = State.Start;
            public GetStockPriceForAsync_StateMachine(StockPrices @this, string companyId)
            {
                this.@this = @this;
                _companyId = companyId;
            }

            public void Start()
            {
                try
                {
                    if (_state == State.Start)
                    {
                        // The code from the start of the method to the first 'await'.

                        if (string.IsNullOrEmpty(_companyId))
                            throw new ArgumentNullException();

                        _initializeMapIfNeededTask = @this.InitializeMapIfNeededAsync();

                        // Update state and schedule continuation
                        _state = State.Step1;
                        _initializeMapIfNeededTask.ContinueWith(_ => Start());
                    }
                    else if (_state == State.Step1)
                    {
                        // Need to check the error and the cancel case first
                        if (_initializeMapIfNeededTask.Status == TaskStatus.Canceled)
                            _tcs.SetCanceled();
                        else if (_initializeMapIfNeededTask.Status == TaskStatus.Faulted)
                            _tcs.SetException(_initializeMapIfNeededTask.Exception!.InnerException!);
                        else
                        {
                            // The code between first await and the rest of the method

                            @this._stockPrices.TryGetValue(_companyId, out var result);
                            _tcs.SetResult(result);
                        }
                    }
                }
                catch (Exception e)
                {
                    _tcs.SetException(e);
                }
            }

            public Task<decimal> Task => _tcs.Task;

        }
    }
}
