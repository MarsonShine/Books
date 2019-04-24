using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace UtilitiesForTAP {
    public class CancellationAsyncMethod {
        protected event EventHandler<object> Click;
        private CancellationTokenSource cts = new CancellationTokenSource();
        public void Cancel(object obj, object obj2) {
            cts.Cancel();
        }

        public async Task Start() {
            DbCommand cmd = new System.Data.SqlClient.SqlCommand();
            Click += Cancel;
            var ret = await cmd.ExecuteNonQueryAsync(cts.Token);
        }

        public async Task Run() {
            var t = Start();
            while (cts.IsCancellationRequested) {

            }
            await t;
        }
    }
}