using System;
using System.IO;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Webview
{
    private const string WebviewFileName = "webview.html";

    private MonoBehaviour parent;
    private WebviewWindowBase webViewObject;
    private int left, top, right, bottom;

    // Event to call when webview starts, receives message.
    public Action<string> OnWebviewStarted;

    // Event to call when avatar is created, receives GLB url.
    public Action<string> OnAvatarCreated;

    /// <summary>
    ///     Create webview object attached to a MonoBehaviour object
    /// </summary>
    /// <param name="parent">Parent game object.</param>
    public void CreateWebview(MonoBehaviour parent)
    {
        this.parent = parent;

        SetWebviewWindow();
        parent.StartCoroutine(LoadWebviewURL());
        SetScreenPadding(left, top, right, bottom);
    }

    /// <summary>
    ///     Set webview screen padding in pixels.
    /// </summary>
    public void SetScreenPadding(int left, int top, int right, int bottom)
    {
        this.left = left;
        this.top = top;
        this.right = right;
        this.bottom = bottom;

        if (webViewObject)
        {
            webViewObject.SetMargins(left, top, right, bottom);
        }
    }

    private void SetWebviewWindow()
    {
        WebviewOptions options = new WebviewOptions();

#if !UNITY_EDITOR && UNITY_ANDROID
        webViewObject = parent.gameObject.AddComponent<AndroidWebViewWindow>();
#elif !UNITY_EDITOR && UNITY_IOS
        webViewObject = parent.gameObject.AddComponent<IOSWebViewWindow>();
#else
        webViewObject = parent.gameObject.AddComponent<NotSupportedWebviewWindow>();
#endif
        webViewObject.OnLoaded = OnLoaded;
        webViewObject.OnJS = OnWebMessageReceived;

        webViewObject.Init(options);
        webViewObject.IsVisible = true;
    }

    private IEnumerator LoadWebviewURL()
    {
        string source = Path.Combine(Application.streamingAssetsPath, WebviewFileName);
        string destination = Path.Combine(Application.persistentDataPath, WebviewFileName);
        byte[] result = null;

#if UNITY_ANDROID
        using(UnityWebRequest request = UnityWebRequest.Get(source))
        {
            yield return request.SendWebRequest();
            result = request.downloadHandler.data;
        }
#elif UNITY_IOS
        result = File.ReadAllBytes(source);
#endif

        File.WriteAllBytes(destination, result);
        yield return null;

        webViewObject.LoadURL($"file://{ destination}");
    }

    private void OnWebMessageReceived(string message)
    {
        Debug.Log(message);

        if (message.Contains(".glb"))
        {
            UnityEngine.Object.Destroy(webViewObject);
            OnAvatarCreated?.Invoke(message);
        }
    }

    private void OnLoaded(string message)
    {
        webViewObject.EvaluateJS(@"
            if (window && window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
                window.Unity = {
                    call: function(msg) { 
                        window.webkit.messageHandlers.unityControl.postMessage(msg); 
                    }
                }
            } 
            else {
                window.Unity = {
                    call: function(msg) {
                        window.location = 'unity:' + msg;
                    }
                }
            }
        ");
    }
}
