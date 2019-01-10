using System;

namespace TAPs {
    internal class PermissionDialog {
        public System.Func<object> Closed { get; internal set; }
        public bool PermissionGranted { get; internal set; }

        internal void Show() {
            throw new NotImplementedException();
        }
    }
}