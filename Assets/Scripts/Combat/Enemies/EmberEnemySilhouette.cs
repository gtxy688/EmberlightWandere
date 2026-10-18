using UnityEngine;
namespace Emberlight
{
    public sealed class EmberEnemySilhouette : MonoBehaviour
    {
        [System.Serializable]
        struct Skin
        {
            public int kind;
            public Sprite sprite;
            public float height;
        }

        [SerializeField] Sprite ordinarySprite;
        [SerializeField] SpriteRenderer ordinaryBody;
        [SerializeField] Skin[] skins;
        [SerializeField] Sprite directionalShieldSprite;
        Transform[] parts;
        Sprite[] sprites;
        bool[] active, flipped;
        Vector3[] positions, scales;
        Quaternion[] rotations;
        Color[] colors;
        Transform decoration;
        MaterialPropertyBlock skinProperties;
        static readonly int MainTextureId = Shader.PropertyToID("_MainTex");
        static readonly int HitFlashId = Shader.PropertyToID("_HitFlash");
        readonly Transform[] roles = new Transform[9];
        public bool UsesWholeBodySkin { get; private set; }
        public SpriteRenderer WholeBody => UsesWholeBodySkin ? ordinaryBody : null;
        public Sprite DirectionalShieldSprite => directionalShieldSprite;

        public bool HasSkinFor(int kind) => ordinaryBody != null && TrySkin(kind, out _);

        bool TrySkin(int kind, out Skin result)
        {
            if (skins != null)
                for (int i = 0; i < skins.Length; i++)
                    if (skins[i].kind == kind && skins[i].sprite != null && skins[i].height > 0)
                    { result = skins[i]; return true; }
            result = default;
            return false;
        }
        Transform Role(int kind)
        {
            var g=new GameObject("Role silhouette " + kind).transform;g.SetParent(decoration,false);roles[kind]=g;return g;
        }
        SpriteRenderer Accent(Transform parent,string name,float x,float y,float w,float h,Color color,int order=4)
        { return EmberVisuals.Shape(name,parent,new Vector2(x,y),new Vector2(w,h),color,order); }
        static readonly Unity.Profiling.ProfilerMarker RolesMarker = new Unity.Profiling.ProfilerMarker("Ember.Enemy.Silhouette.BuildRoles");

        void BuildRoles()
        {
            using (RolesMarker.Auto())
            {
                var purple=new Color(.33f,.20f,.44f);
                var g=Role(4);
                Accent(g,"Long cowl",0,.45f,.32f,.40f,purple);
                Accent(g,"Collar",0,.06f,.58f,.12f,new Color(.64f,.44f,.72f),5);
                g=Role(5);
                Accent(g,"Brood belly",0,-.10f,.70f,.55f,new Color(.27f,.34f,.13f),5);
                for(int i=-1;i<=1;i++)
                {
                    Accent(g,"Egg sac",i*.20f,-.15f,.20f,.28f,new Color(.53f,.62f,.25f),6);
                    Accent(g,"Sleeping brood eye",i*.20f,-.13f,.045f,.075f,new Color(.88f,.94f,.46f),7);
                }
                g=Role(6);
                for(int i=-1;i<=1;i+=2)
                {
                    var a=Accent(g,"Feelers",i*.19f,.45f,.055f,.26f,new Color(.52f,.66f,.26f));a.transform.localRotation=Quaternion.Euler(0,0,-i*28);
                    Accent(g,"Feeler light",i*.25f,.55f,.09f,.09f,new Color(.86f,.93f,.4f),5);
                }
                g=Role(7);
                for(int i=-1;i<=1;i+=2)Accent(g,"Armor shoulder",i*.31f,.06f,.28f,.32f,new Color(.33f,.48f,.53f),5);
                Accent(g,"Helmet brow",0,.36f,.60f,.12f,new Color(.48f,.61f,.63f),7);
                Accent(g,"Armor clasp",0,-.10f,.13f,.16f,new Color(.64f,.73f,.71f),6);
                g=Role(8);
                Accent(g,"Ember belly",0,-.05f,.60f,.59f,new Color(.39f,.15f,.095f),5);
                var seam=Accent(g,"Hot seam",0,-.10f,.15f,.28f,new Color(1f,.45f,.10f),6);seam.sprite=EmberArt.Flame;
                var fuse=Accent(g,"Fuse",.09f,.52f,.06f,.28f,new Color(.78f,.53f,.23f),5);fuse.transform.localRotation=Quaternion.Euler(0,0,-22);
                g=Role(3);
                for(int i=-2;i<=2;i++)Accent(g,"Ragged mantle",i*.145f,-.26f,.22f,.35f+(.06f*(i%2)),new Color(.18f,.16f,.25f),2).sprite=EmberCharacterArt.Hem;
                for(int i=-1;i<=1;i+=2)Accent(g,"Mantle shoulder",i*.33f,.08f,.26f,.31f,new Color(.27f,.24f,.36f),4);
            }
        }
        static readonly Unity.Profiling.ProfilerMarker CaptureMarker = new Unity.Profiling.ProfilerMarker("Ember.Enemy.Silhouette.Initialize");

        void Capture()
        {
            using (CaptureMarker.Auto())
            {
                int count=transform.childCount;
                parts=new Transform[count];positions=new Vector3[count];scales=new Vector3[count];rotations=new Quaternion[count];colors=new Color[count];
                sprites=new Sprite[count];active=new bool[count];flipped=new bool[count];
                for(int i=0;i<count;i++)
                {
                    parts[i]=transform.GetChild(i);positions[i]=parts[i].localPosition;
                    scales[i]=parts[i].localScale;rotations[i]=parts[i].localRotation;
                    active[i]=parts[i].gameObject.activeSelf;
                    var renderer=parts[i].GetComponent<SpriteRenderer>();if(renderer!=null){colors[i]=renderer.color;sprites[i]=renderer.sprite;flipped[i]=renderer.flipX;}
                }
            }
        }

        void EnsureDecoration()
        {
            if (decoration != null) return;
            decoration=new GameObject("Silhouette").transform;decoration.SetParent(transform,false);
            // 仅旧样板需要这些分层配饰；整体皮肤不创建隐藏的重复部件。
            for(int i=0;i<3;i++) EmberVisuals.Shape("Hem",decoration,new Vector2((i-1)*.18f,-.30f),new Vector2(.24f,.26f),Color.white,2).sprite=EmberCharacterArt.Hem;
            for(int i=0;i<2;i++) EmberVisuals.Shape("Tail",decoration,new Vector2(-.15f-i*.10f,-.26f-i*.11f),new Vector2(.24f-i*.045f,.40f),Color.white,2).sprite=EmberCharacterArt.Hem;
            for(int i=-1;i<=1;i+=2) EmberVisuals.Shape("Shoulder",decoration,new Vector2(i*.32f,.06f),new Vector2(.25f,.35f),Color.white,4);
            BuildRoles();
        }
        public void Apply(int kind,Color body)
        {
            if(parts==null)Capture();
            UsesWholeBodySkin = false;
            for(int i=0;i<parts.Length;i++)
            {
                var p=parts[i];p.localPosition=positions[i];p.localScale=scales[i];p.localRotation=rotations[i];
                p.gameObject.SetActive(active[i]);
                var renderer=p.GetComponent<SpriteRenderer>();if(renderer!=null){renderer.color=colors[i];renderer.sprite=sprites[i];renderer.flipX=flipped[i];}
                if(renderer!=null && p.name=="Cloak")renderer.color=body;
                if(renderer!=null && p.name=="Hood")renderer.color=body*1.2f;
            }
            if (ordinaryBody != null && TrySkin(kind, out var skin))
            {
                foreach (var part in parts)
                    if (part.GetComponent<SpriteRenderer>() != null && part != ordinaryBody.transform && part.name != "Shadow")
                        part.gameObject.SetActive(false);
                ordinaryBody.sprite = skin.sprite;
                ordinaryBody.color = Color.white;
                // 只重置命中参数并绑定本次皮肤；清空整个块会丢失 Sprite 的纹理绑定。
                if (skinProperties == null) skinProperties = new MaterialPropertyBlock();
                ordinaryBody.GetPropertyBlock(skinProperties);
                skinProperties.SetTexture(MainTextureId, skin.sprite.texture);
                skinProperties.SetFloat(HitFlashId, 0);
                ordinaryBody.SetPropertyBlock(skinProperties);
                ordinaryBody.transform.localPosition = new Vector3(0, .12f, 0);
                ordinaryBody.transform.localScale = Vector3.one * (skin.height / skin.sprite.bounds.size.y);
                if (decoration != null) decoration.gameObject.SetActive(false);
                UsesWholeBodySkin = true;
                return;
            }
            // 未绑定皮肤的旧样板继续使用原有轮廓，复用时恢复 Sprite 与显隐。
            if(kind==0 && ordinarySprite!=null && ordinaryBody!=null)
            {
                foreach(var p in parts)
                    if(p.GetComponent<SpriteRenderer>()!=null && p!=ordinaryBody.transform && p.name!="Shadow")p.gameObject.SetActive(false);
                ordinaryBody.sprite=ordinarySprite;
                ordinaryBody.color=new Color(.82f,.82f,.88f);
                ordinaryBody.transform.localPosition=new Vector3(0,.12f,0);
                ordinaryBody.transform.localScale=Vector3.one*(1.25f/ordinarySprite.bounds.size.y);
                if (decoration != null) decoration.gameObject.SetActive(false);
                return;
            }
            EnsureDecoration();
            decoration.gameObject.SetActive(true);
            for(int i=3;i<roles.Length;i++)if(roles[i]!=null)roles[i].gameObject.SetActive(kind==i);
            for(int i=0;i<7;i++)
            {
                var p=decoration.GetChild(i);
                p.gameObject.SetActive(kind==0?i<3:kind==1?i>=3&&i<5:kind==2&&i>=5);
                p.GetComponent<SpriteRenderer>().color=Color.Lerp(body,Color.black,kind==2?.12f:.06f);
            }
            if(kind>2)
            {
                foreach(var p in parts)
                {
                    if(p.name=="Cloak")p.localScale=kind==4?new Vector3(.49f,.94f,1):kind==5||kind==8?new Vector3(.78f,.78f,1):kind==6?new Vector3(.56f,.58f,1):new Vector3(.76f,.82f,1);
                    if(p.name=="Hood" && kind==7)p.localScale=new Vector3(.70f,.53f,1);
                    if(p.name=="Eye")p.GetComponent<SpriteRenderer>().color=kind==4?new Color(.84f,.60f,1):kind==5||kind==6?new Color(.87f,.95f,.40f):kind==7?new Color(.60f,.90f,1):new Color(1,.48f,.23f);
                }
                return;
            }
            foreach(var p in parts)
            {
                if(p.name=="Cloak")p.localScale=kind==1?new Vector3(.46f,.85f,1):kind==2?new Vector3(.80f,.76f,1):new Vector3(.61f,.72f,1);
                if(p.name=="Hood")p.localScale=kind==1?new Vector3(.51f,.57f,1):kind==2?new Vector3(.78f,.51f,1):new Vector3(.66f,.60f,1);
                if(p.name=="Face")
                {
                    p.localScale=kind==2?new Vector3(.53f,.22f,1):kind==1?new Vector3(.33f,.28f,1):new Vector3(.43f,.31f,1);
                    p.GetComponent<SpriteRenderer>().color=new Color(.035f,.045f,.07f);
                }
                if(p.name=="Eye")
                {
                    p.localScale=kind==2?new Vector3(.095f,.045f,1):kind==1?new Vector3(.055f,.09f,1):new Vector3(.063f,.072f,1);
                    p.GetComponent<SpriteRenderer>().color=kind==2?new Color(1,.52f,.23f):kind==1?new Color(1,.48f,.58f):new Color(.95f,.40f,.36f);
                }
            }
        }
    }
}
