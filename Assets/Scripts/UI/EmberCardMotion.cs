using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Emberlight
{
    public sealed class EmberCardMotion : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        CanvasGroup group;Vector3 target=Vector3.one;float delay,age;bool selected;Image face;
        public void Initialize(float entranceDelay,Image background){StopAllCoroutines();if(group==null)group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();selected=false;age=0;target=Vector3.one;background.color=Color.white;group.alpha=0;delay=entranceDelay;face=background;transform.localScale=Vector3.one*.94f;}
        void OnDisable(){StopAllCoroutines();selected=false;target=Vector3.one;}
        void Update(){age+=Time.unscaledDeltaTime;if(group!=null)group.alpha=Mathf.Clamp01((age-delay)/.22f);transform.localScale=Vector3.Lerp(transform.localScale,target,Time.unscaledDeltaTime*18);}
        public void OnPointerDown(PointerEventData e){if(!selected)target=Vector3.one*.97f;}
        public void OnPointerUp(PointerEventData e){if(!selected)target=Vector3.one;}
        public void OnPointerExit(PointerEventData e){if(!selected)target=Vector3.one;}
        public void Select(System.Action finish){if(selected)return;selected=true;StartCoroutine(Flash(finish));}
        IEnumerator Flash(System.Action finish){target=Vector3.one*1.035f;Color original=face.color;for(float t=0;t<.18f;t+=Time.unscaledDeltaTime){face.color=Color.Lerp(new Color(.65f,.4f,.12f),original,t/.18f);yield return null;}finish();}
    }
}
