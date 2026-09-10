using UnityEngine;
using System;
using System.Globalization;

// Nota per lo sviluppatore: Assicurati di aver importato https://github.com/gree/unity-webview prima di usare questo script.
namespace AdSwapNetwork
{
    public class AdSwap : MonoBehaviour
    {
        private static string _pubId;
        private static string _baseUrl = "https://adswap.netlify.app/ad.html";

        public static void Initialize(string publisherId)
        {
            _pubId = publisherId;
            Debug.Log($"[AdSwap SDK] Initialized with PubID: {_pubId}");
        }

        public static void ShowInterstitial(string category = "any", string geo = null)
        {
            if (string.IsNullOrEmpty(_pubId))
            {
                Debug.LogError("[AdSwap SDK] Error: You must call Initialize() first.");
                return;
            }

            // Crea un GameObject invisibile per ospitare la WebView
            GameObject adObject = new GameObject("AdSwap_Interstitial");
            WebViewObject webView = adObject.AddComponent<WebViewObject>();

            // Gestisce le callback in uscita dal JS
            webView.Init(
                cb: (msg) => {
                    if (msg == "close") {
                        Destroy(adObject);
                    } 
                    else if (msg.StartsWith("open:")) {
                        // SICUREZZA CLICK: Apre il link sul browser esterno del telefono
                        Application.OpenURL(msg.Substring(5));
                    }
                },
                enableWKWebView: true,
                transparent: true
            );

            // Costruisci URL
            string userLocale = string.IsNullOrEmpty(geo) ? RegionInfo.CurrentRegion.TwoLetterISORegionName.ToLower() : geo;
            string url = $"{_baseUrl}?pubId={_pubId}&format=interstitial&category={category}&platform=unity&geo={userLocale}";

            // Riempi lo schermo intero
            webView.SetMargins(0, 0, 0, 0);
            webView.SetVisibility(true);
            webView.LoadURL(url);

            // JS INJECTION: Iniettiamo i bridge al volo
            webView.EvaluateJS(@"
                window.AdSwapAndroid = {
                    closeAd: function() { Unity.call('close'); },
                    reportAd: function(id) { console.log('Reported: ' + id); }
                };
                window.open = function(url) { Unity.call('open:' + url); };
            ");
        }
    }
}
