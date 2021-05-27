# Unity WebView for Mobile

```csharp
public class WebviewTest : MonoBehaviour
{
   private void Start()
   {
      Webview webview = new Webview();
      webview.SetScreenPadding(0, 0, 0, 0);
      webview.CreateWebview(this);
   }
}
```
