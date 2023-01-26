using System;

namespace ConnectToUrl;

internal class DisposableAction : IDisposable {
    public static DisposableAction Noop { get; } = new DisposableAction(() => { });

    private readonly Action _action;
    private Boolean _hasRun;

    public DisposableAction(Action action) {
        _action = action;
    }

    public void Dispose() {
        if (!_hasRun) {
            _hasRun = true;
            _action();
        }
    }
}