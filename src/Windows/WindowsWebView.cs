#if WEBVIEW
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using static OpenConnect;

namespace ConnectToUrl.Windows;

[SupportedOSPlatform("Windows")]
internal unsafe class WindowsWebView : IWebView {
    private readonly openconnect_open_webview_vfn CallbackDelegate = Callback;

    public void Attach(openconnect_info* vpninfo) {
        openconnect_set_webview_callback(vpninfo, CallbackDelegate);
    }

    private static Int32 Callback(openconnect_info* vpninfo, Char* uri, void* privdata) {
        var result = DialogResult.None;
        var urlString = Helper.PtrToStringAnsi(uri);
        if (urlString == null) {
            return -1;
        }

        var thread = new Thread(() => {
            var form = new Form();
            form.Text = "Single sign-on";

            form.Load += (_, loadArgs) => {
                var scr = Screen.PrimaryScreen;
                if (scr != null) {
                    form.Size = scr.WorkingArea.Size / 2;
                    if (form.Size.Height < 720) {
                        // min 720px height
                        form.Size = form.Size with {
                            Height = 720,
                        };
                    }

                    form.Location = new Point(
                        scr.Bounds.X + (scr.WorkingArea.Width - form.Size.Width) / 2,
                        scr.Bounds.Y + (scr.WorkingArea.Height - form.Size.Height) / 2
                    );
                }
            };

            var control = new WebView2();
            control.Location = new Point(0, 0);
            control.Size = form.ClientSize;

            control.CoreWebView2InitializationCompleted += (sender, initArgs) => {
                if (initArgs.InitializationException != null) {
                    Console.Error.WriteLine($"CoreWebView2InitializationCompleted: {initArgs.InitializationException}");
                    form.DialogResult = DialogResult.Abort;
                    return;
                }

                control.CoreWebView2!.WebResourceResponseReceived += (_, responseArgs) => {
                    var webviewResult = Helper.AllocHGlobal<oc_webview_result>();
                    webviewResult->uri = Helper.StringToHGlobalAnsi(responseArgs.Request.Uri);

                    var cookieHeaders = responseArgs.Response!.Headers!.GetHeaders("Set-Cookie");
                    var parsedCookies = new List<KeyValuePair<String, String>>();
                    while (cookieHeaders!.MoveNext()) {
                        var cookie = cookieHeaders.Current.Value!.Split('=', 2);
                        var cookieName = cookie[0];
                        var cookieValue = cookie[1];

                        if (cookieValue.Contains(';')) {
                            cookieValue = cookieValue[..cookieValue.IndexOf(';')];
                        }

                        parsedCookies.Add(new KeyValuePair<String, String>(cookieName, cookieValue));
                    }

                    // A pair of pointers for every cookie (key + value), and an
                    // terminating null.
                    webviewResult->cookies = (Char**)Marshal.AllocHGlobal(sizeof(Char*) * (parsedCookies.Count * 2 + 1));
                    webviewResult->cookies[parsedCookies.Count * 2] = null;
                    for (var idx = 0; idx < parsedCookies.Count; ++idx) {
                        webviewResult->cookies[idx * 2 + 0] = Helper.StringToHGlobalAnsi(parsedCookies[idx].Key);
                        webviewResult->cookies[idx * 2 + 1] = Helper.StringToHGlobalAnsi(parsedCookies[idx].Value);
                    }

                    var res = openconnect_webview_load_changed(vpninfo, webviewResult);

                    for (var idx = 0; idx < parsedCookies.Count; ++idx) {
                        Helper.FreeHGlobal(ref webviewResult->cookies[idx * 2 + 0]);
                        Helper.FreeHGlobal(ref webviewResult->cookies[idx * 2 + 1]);
                    }

                    Helper.FreeHGlobal(ref webviewResult->cookies);
                    Helper.FreeHGlobal(ref webviewResult->uri);
                    Helper.FreeHGlobal(ref webviewResult);

                    if (res == -EINVAL) {
                        // vpninfo->quit_reason is probably set
                        Console.Error.WriteLine("openconnect_webview_load_changed returned -EINVAL");
                        form.DialogResult = DialogResult.Abort;
                        return;
                    }

                    if (res == -EAGAIN) {
                        // Keep going.
                        return;
                    }

                    form.DialogResult = DialogResult.OK;
                };
            };

            form.Resize += (_, _) => {
                control.Size = form.ClientSize;
            };

            form.Controls.Add(control);

            var options = new CoreWebView2EnvironmentOptions();
            options.AllowSingleSignOnUsingOSPrimaryAccount = true;

            var environment = Wait(CoreWebView2Environment.CreateAsync(options: options));
            if (environment == null) {
                Console.Error.WriteLine("CoreWebView2Environment.CreateAsync returned null");
                result = DialogResult.Abort;
                return;
            }

            Wait(control.EnsureCoreWebView2Async(environment));

            control.CoreWebView2.Settings.AreHostObjectsAllowed = false;
            control.CoreWebView2.Settings.IsWebMessageEnabled = false;
            control.Source = new Uri(urlString);

            result = form.ShowDialog();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Console.WriteLine($"WebViewCallback: DialogResult={result}");
        if (result == DialogResult.OK) {
            return 0;
        }

        return -1;
    }

    private static void Wait(Task? task) {
        while (task is { IsCompleted: false }) {
            Application.DoEvents();
        }
    }

    private static T Wait<T>(Task<T>? task) {
        while (task is { IsCompleted: false }) {
            Application.DoEvents();
        }

        return task!.Result;
    }
}

#endif