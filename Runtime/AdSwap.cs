using UnityEngine;
using System;

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

        // =========================================================
        // INTERSTITIAL CON CALLBACK
        // =========================================================
        public static void ShowInterstitial(string category = "any", string geo = null, Action onAdClosed = null)
        {
            if (string.IsNullOrEmpty(_pubId))
            {
                Debug.LogError("[AdSwap SDK] Error: You must call Initialize() first.");
                onAdClosed?.Invoke(); // Rilascia il blocco del gioco anche in caso di errore
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
                        // 🔥 LANCIA IL CALLBACK PER FAR RIPARTIRE IL GIOCO
                        onAdClosed?.Invoke();
                    } 
                    else if (msg == "loaded") {
                        // Il JS ha caricato il video/immagine. 
                        // La dissolvenza è gestita dal CSS web.
                    }
                    else if (msg.StartsWith("open:")) {
                        // SICUREZZA CLICK: Apre il link sul browser esterno del telefono
                        Application.OpenURL(msg.Substring(5));
                    }
                    else if (msg.StartsWith("info:")) {
                        // Unity non ha alert() nativi. Stampiamo in console per sicurezza
                        Debug.Log("[AdSwap SDK] About this Ad: " + msg.Substring(5));
                    }
                    else if (msg.StartsWith("report:")) {
                        // Ricevuto il tap sulla bandierina. Simula la conferma ed esegue il report.
                        string adId = msg.Substring(7);
                        webView.EvaluateJS($"window.AdSwapSDK.executeReport('{adId}');");
                        Debug.Log("[AdSwap SDK] Report submitted for ad: " + adId);
                    }
                },
                enableWKWebView: true,
                transparent: true
            );

            // Costruisci URL (Il geo è opzionale: se è null, l'SDK JS ricaverà da solo la lingua del device!)
            string url = $"{_baseUrl}?pubId={_pubId}&format=interstitial&category={category}&platform=unity";
            if (!string.IsNullOrEmpty(geo)) {
                url += $"&geo={geo}";
            }

            // Riempi lo schermo intero
            webView.SetMargins(0, 0, 0, 0);
            webView.SetVisibility(true);
            webView.LoadURL(url);

            // 🔥 JS INJECTION: Iniettiamo i bridge mascherandoci da Android
            // In questo modo il file adswap-sdk.js troverà "window.AdSwapAndroid" 
            // e saprà esattamente come parlare con la nostra WebView Unity!
            webView.EvaluateJS(@"
                window.AdSwapAndroid = {
                    closeAd: function() { Unity.call('close'); },
                    adLoaded: function() { Unity.call('loaded'); },
                    reportAd: function(id) { Unity.call('report:' + id); },
                    showInfoDialog: function(msg) { Unity.call('info:' + msg); }
                };
                window.open = function(url) { Unity.call('open:' + url); };
            ");
        }

        // =========================================================
        // BANNER (STUB TEMPORANEO)
        // =========================================================
        public static void ShowBanner(string category = "any", string geo = null)
        {
            Debug.LogWarning("[AdSwap SDK] Unity Native Banners coming soon. Please use ShowInterstitial() for now.");
        }
    }
}
