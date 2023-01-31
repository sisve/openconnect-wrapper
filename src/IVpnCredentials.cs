using System;

namespace ConnectToUrl; 

internal interface IVpnCredentials {
    String Username { get; }
    String Password { get; }
    void Fail();
    void Success();
}