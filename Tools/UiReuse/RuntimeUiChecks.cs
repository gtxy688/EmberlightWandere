using System;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Emberlight;

[InitializeOnLoad]
public static class RuntimeUiChecks
{
    static IEnumerator run;
    static double deadline;
    static string error;
    static RuntimeUiChecks() { EditorApplication.playModeStateChanged += state => {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("RuntimeUiChecks", false)) {
            run = Check(); deadline = EditorApplication.timeSinceStartup + 60;
            Application.logMessageReceived += (text, stack, type) => { if (type == LogType.Error || type == LogType.Exception) error = text; };
            EditorApplication.update += Tick;
        }
    }; }
    public static void Run() { SessionState.SetBool("RuntimeUiChecks",true); EditorApplication.isPlaying = true; }
    static void Tick() {
        try {
            Assert(error == null, error);
            Assert(EditorApplication.timeSinceStartup < deadline,"timeout");
            if (run.MoveNext()) return;
            File.WriteAllText("result.txt","PASS: 20 repeated settings/card opens retain object identity; latest callback, confirmation reset, silent slider sync, 3/7 card layout, selection cancellation and single delivery; HUD rendered.");
            EditorApplication.Exit(0);
        } catch(Exception ex) { File.WriteAllText("result.txt","FAIL: " + ex); EditorApplication.Exit(1); }
    }
    static void Assert(bool value,string text) { if(!value) throw new Exception(text); }
    static int[] Ids(GameObject g) { return g.GetComponentsInChildren<Transform>(true).Select(t=>t.GetInstanceID()).OrderBy(x=>x).ToArray(); }
    static IEnumerator Check() {
        var font = EmberFonts.CreateChinese();
        var host = new GameObject("Checks",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas = host.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = host.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(540,960);
        var settings = host.AddComponent<EmberSettingsPanel>(); settings.Initialize(font);
        var settingsIds = Ids(host);
        int closed=0,camp=0,sliderEvents=0;
        foreach(var slider in host.GetComponentsInChildren<Slider>(true)) slider.onValueChanged.AddListener(v=>sliderEvents++);
        for(int i=0;i<20;i++) { settings.Show(()=>closed++,true,()=>camp++); settings.Hide(); }
        Assert(settingsIds.SequenceEqual(Ids(host)),"settings hierarchy changed");
        Assert(sliderEvents==0,"Show notified sliders");
        settings.Show(()=>closed++,true,()=>camp++);
        var campButton=host.GetComponentsInChildren<Button>(true).First(b=>b.name=="Button 返回营地");
        campButton.onClick.Invoke(); settings.Hide(); settings.Show(()=>closed++,true,()=>camp++); campButton.onClick.Invoke();
        Assert(camp==0,"confirmation retained"); campButton.onClick.Invoke(); Assert(camp==1,"camp callback");
        settings.Show(()=>closed+=10,false); host.GetComponentsInChildren<Button>(true).First(b=>b.name=="Button 返回").onClick.Invoke(); Assert(closed==10,"stale close callback");
        settings.Hide();
        var panel=host.AddComponent<EmberUpgradePanel>(); panel.Initialize(font);
        var ids=Ids(host); var progress=new RunProgress();
        var seven=Enumerable.Range(0,7).Select(i=>new EmberOffer(RunProgress.StatShield,EmberRarity.Gold)).ToArray();
        var names=new string[5]; names[4]="护盾"; var desc=new string[5];
        int selected=0;
        for(int i=0;i<20;i++) { panel.Show(i%2==0?seven:seven.Take(3).ToArray(),progress,names,desc,o=>selected++); panel.Hide(); }
        Assert(ids.SequenceEqual(Ids(host)),"card hierarchy changed");
        panel.Show(seven,progress,names,desc,o=>selected++);
        var scroll=host.GetComponentInChildren<ScrollRect>(); Assert(scroll!=null,"seven missing scroll");
        scroll.content.anchoredPosition=new Vector2(0,200); scroll.velocity=new Vector2(0,100);
        panel.Hide(); panel.Show(seven,progress,names,desc,o=>selected++);
        Assert(scroll.content.anchoredPosition==Vector2.zero && scroll.velocity==Vector2.zero,"scroll not reset");
        var card=host.GetComponentsInChildren<Button>().First(b=>b.name.StartsWith("Blessing card"));
        card.onClick.Invoke(); panel.Hide();
        for(int i=0;i<30;i++) yield return null;
        Assert(selected==0,"hidden animation delivered");
        panel.Show(seven.Take(3).ToArray(),progress,names,desc,o=>selected+=10);
        card=host.GetComponentsInChildren<Button>().First(b=>b.name.StartsWith("Blessing card")); card.onClick.Invoke(); card.onClick.Invoke();
        var until=EditorApplication.timeSinceStartup+1; while(EditorApplication.timeSinceStartup<until) yield return null;
        Assert(selected==10,"latest selection not delivered once");
        panel.Hide();
        var hud=new EmberHud(host.transform,font,()=>{}); hud.Set(80,100,progress,62,48,4,10,20); hud.Stage(4,25,"休闲","渐入长夜");
        yield return null;
        Canvas.ForceUpdateCanvases();
        var camera = new GameObject("Capture",typeof(Camera)).GetComponent<Camera>();
        camera.transform.position = new Vector3(0,0,-1000);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f,.04f,.055f);
        camera.nearClipPlane = .1f; camera.farClipPlane = 10;
        var rt = new RenderTexture(540,960,24); camera.targetTexture=rt;
        canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
        Canvas.ForceUpdateCanvases(); camera.Render();
        var previous=RenderTexture.active; RenderTexture.active=rt;
        var image=new Texture2D(540,960,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,540,960),0,0); image.Apply();
        File.WriteAllBytes("hud.png",image.EncodeToPNG()); RenderTexture.active=previous;
        UnityEngine.Object.Destroy(image); camera.targetTexture=null; rt.Release(); UnityEngine.Object.Destroy(rt);
        Assert(File.Exists("hud.png"),"capture missing");
        for(int i=0;i<10;i++) yield return null;
    }
}
