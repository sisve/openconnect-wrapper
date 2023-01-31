using static OpenConnect;

namespace ConnectToUrl; 

internal unsafe interface IWebView {
    void Attach(openconnect_info* vpninfo);
}