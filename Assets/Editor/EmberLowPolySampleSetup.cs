using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Emberlight.Editor
{
    /// <summary>保存真实 Mesh、材质、骨架和独立场景，运行不生成模型。</summary>
    public static class EmberLowPolySampleSetup
    {
        public const string Root = "Assets/Art/LowPolySample";
        public const string ScenePath = Root + "/LowPolyCourtyard.unity";
        enum Paint { Cloth, ClothLight, Gold, Leather, Face, Flame, Stone, FloorLight, FloorDark, Moss, Bark, Foliage, Wax, Iron }
        static Material[] materials;

        [MenuItem("Emberlight/Art/Build Low Poly Prototype")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            foreach (string folder in new[] { Root, Root + "/Meshes", Root + "/Materials", Root + "/Prefabs" }) Folder(folder);
            materials = Palette();
            Keeper();
            Courtyard();
            CreateScene();
            AssetDatabase.SaveAssets();
            Debug.Log("LOW_POLY_SAMPLE_READY: persistent 3D courtyard, skinned keeper and independent preview scene.");
        }

        [MenuItem("Emberlight/Art/Open Low Poly Courtyard")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            EditorSceneManager.OpenScene(ScenePath);
        }

        static Material[] Palette()
        {
            var colors = new[] { new Color(.12f,.15f,.24f), new Color(.20f,.25f,.34f), new Color(.67f,.43f,.15f),
                new Color(.19f,.12f,.09f), new Color(.022f,.03f,.045f), new Color(1,.52f,.08f),
                new Color(.25f,.30f,.35f), new Color(.31f,.37f,.40f), new Color(.28f,.335f,.36f),
                new Color(.19f,.28f,.24f), new Color(.23f,.15f,.10f), new Color(.11f,.21f,.20f),
                new Color(.75f,.67f,.43f), new Color(.10f,.13f,.16f) };
            var result = new Material[colors.Length];
            var shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Built-in Standard shader is missing.");
            for (int i = 0; i < result.Length; i++)
            {
                string path = Root + "/Materials/" + (Paint)i + ".mat";
                result[i] = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (result[i] != null) continue;
                var material = new Material(shader) { name = ((Paint)i).ToString(), color = colors[i] };
                material.SetFloat("_Glossiness", .12f);
                material.SetFloat("_Metallic", i == (int)Paint.Gold ? .32f : 0);
                if (i == (int)Paint.Flame)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", colors[i] * 1.6f);
                }
                AssetDatabase.CreateAsset(material, path);
                result[i] = material;
            }
            return result;
        }

        static GameObject Keeper()
        {
            var saved = Load("Keeper");
            if (saved != null) return saved;
            var root = new GameObject("Low-poly keeper");
            try
            {
                var bones = new List<Transform> { root.transform };
                var pelvis = Bone("Pelvis", root.transform, new Vector3(0,.72f,0), bones);
                var chest = Bone("Chest", pelvis, new Vector3(0,.31f,0), bones);
                var head = Bone("Head", chest, new Vector3(0,.28f,0), bones);
                var cape = Bone("Cape", chest, new Vector3(0,.11f,-.12f), bones);
                var leftHip = Bone("Left thigh", pelvis, new Vector3(-.16f,0,0), bones);
                var leftKnee = Bone("Left shin", leftHip, Vector3.down * .36f, bones);
                var leftFoot = Bone("Left foot", leftKnee, Vector3.down * .36f, bones);
                var rightHip = Bone("Right thigh", pelvis, new Vector3(.16f,0,0), bones);
                var rightKnee = Bone("Right shin", rightHip, Vector3.down * .36f, bones);
                var rightFoot = Bone("Right foot", rightKnee, Vector3.down * .36f, bones);
                var leftArm = Bone("Left upper arm", chest, new Vector3(-.29f,.12f,0), bones);
                leftArm.localRotation = Quaternion.Euler(8,0,-10);
                var leftForearm = Bone("Left forearm", leftArm, Vector3.down * .24f, bones);
                leftForearm.localRotation = Quaternion.Euler(-10,0,0);
                var leftHand = Bone("Left hand", leftForearm, Vector3.down * .22f, bones);
                var rightArm = Bone("Right upper arm", chest, new Vector3(.29f,.12f,0), bones);
                rightArm.localRotation = Quaternion.Euler(-8,0,12);
                var rightForearm = Bone("Right forearm", rightArm, Vector3.down * .24f, bones);
                rightForearm.localRotation = Quaternion.Euler(-35,0,0);
                var rightHand = Bone("Right hand", rightForearm, Vector3.down * .22f, bones);
                var lantern = Bone("Lantern", rightHand, new Vector3(0,-.025f,0), bones);
                var model = new EmberLowPolyMeshBuilder();
                int Id(Transform bone) => bones.IndexOf(bone);
                Matrix4x4 At(Transform bone, Vector3 point, Vector3 scale) => bone.localToWorldMatrix * Matrix4x4.TRS(point, Quaternion.identity, scale);

                model.Frustum(At(chest,new Vector3(0,-.12f,0),new Vector3(1,1,.72f)), .31f,.22f,.50f,8,(int)Paint.Cloth,Id(chest));
                model.Frustum(At(chest,new Vector3(0,-.36f,0),new Vector3(1,1,.72f)), .315f,.31f,.025f,8,(int)Paint.Gold,Id(chest));
                model.Frustum(At(chest,new Vector3(0,-.10f,0),new Vector3(1,1,.75f)), .245f,.245f,.055f,8,(int)Paint.Leather,Id(chest));
                model.Box(At(chest,new Vector3(0,-.10f,.19f),Vector3.one),new Vector3(.08f,.07f,.025f),(int)Paint.Gold,Id(chest));
                model.Ellipsoid(At(chest,new Vector3(0,.14f,0),Vector3.one),new Vector3(.26f,.13f,.20f),(int)Paint.ClothLight,Id(chest));
                model.Ellipsoid(At(chest,new Vector3(0,.09f,.20f),Vector3.one),new Vector3(.055f,.075f,.025f),(int)Paint.Gold,Id(chest),4,3);
                model.Hood(head.localToWorldMatrix,(int)Paint.Cloth,(int)Paint.Gold,Id(head));
                model.Ellipsoid(At(head,new Vector3(0,.075f,.259f),Vector3.one),new Vector3(.25f,.262f,.037f),(int)Paint.Face,Id(head));
                foreach (float x in new[] { -.09f,.09f })
                    model.Ellipsoid(At(head,new Vector3(x,.05f,.304f),Vector3.one),new Vector3(.035f,.050f,.014f),(int)Paint.Flame,Id(head),6,4);

                foreach (var thigh in new[] { leftHip,rightHip })
                    model.Frustum(At(thigh,Vector3.down * .16f,Vector3.one),.075f,.105f,.33f,6,(int)Paint.ClothLight,Id(thigh));
                foreach (var shin in new[] { leftKnee,rightKnee })
                {
                    model.Ellipsoid(shin.localToWorldMatrix,new Vector3(.082f,.084f,.085f),(int)Paint.Cloth,Id(shin));
                    model.Frustum(At(shin,Vector3.down * .16f,Vector3.one),.065f,.08f,.32f,6,(int)Paint.Leather,Id(shin));
                }
                foreach (var foot in new[] { leftFoot,rightFoot })
                {
                    model.Box(At(foot,new Vector3(0,-.045f,.055f),Vector3.one),new Vector3(.17f,.11f,.28f),(int)Paint.Leather,Id(foot));
                    model.Box(At(foot,new Vector3(0,-.005f,.085f),Vector3.one),new Vector3(.13f,.025f,.035f),(int)Paint.Gold,Id(foot));
                }
                foreach (var arm in new[] { leftArm,rightArm })
                {
                    model.Ellipsoid(arm.localToWorldMatrix,new Vector3(.13f,.13f,.13f),(int)Paint.ClothLight,Id(arm));
                    model.Frustum(At(arm,Vector3.down * .12f,Vector3.one),.085f,.105f,.24f,6,(int)Paint.Cloth,Id(arm));
                }
                foreach (var forearm in new[] { leftForearm,rightForearm })
                {
                    model.Frustum(At(forearm,Vector3.down * .10f,Vector3.one),.095f,.075f,.21f,6,(int)Paint.ClothLight,Id(forearm));
                    model.Frustum(At(forearm,Vector3.down * .20f,Vector3.one),.10f,.10f,.025f,6,(int)Paint.Gold,Id(forearm));
                }
                foreach (var hand in new[] { leftHand,rightHand })
                    model.Ellipsoid(hand.localToWorldMatrix,new Vector3(.075f,.09f,.07f),(int)Paint.Leather,Id(hand));
                var capeMatrix = cape.localToWorldMatrix;
                var a = capeMatrix.MultiplyPoint3x4(new Vector3(-.20f,0,0));
                var b = capeMatrix.MultiplyPoint3x4(new Vector3(.20f,0,0));
                var c = capeMatrix.MultiplyPoint3x4(new Vector3(.36f,-.56f,-.22f));
                var d = capeMatrix.MultiplyPoint3x4(new Vector3(-.36f,-.56f,-.22f));
                model.Quad(a,b,c,d,(int)Paint.Cloth,Id(cape),true);
                model.Quad(Vector3.Lerp(d,a,.03f),Vector3.Lerp(c,b,.03f),c,d,(int)Paint.Gold,Id(cape),true);
                Lantern(model, lantern.localToWorldMatrix, Id(lantern));

                var mesh = model.Build("Keeper skinned mesh",true,out var slots);
                mesh.bindposes = bones.Select(bone => bone.worldToLocalMatrix * root.transform.localToWorldMatrix).ToArray();
                var skin = root.AddComponent<SkinnedMeshRenderer>();
                skin.sharedMesh = SaveMesh(mesh,"Keeper");
                skin.bones = bones.ToArray(); skin.rootBone = root.transform;
                skin.sharedMaterials = slots.Select(index => materials[index]).ToArray();
                skin.shadowCastingMode = ShadowCastingMode.On;
                skin.receiveShadows = true; skin.updateWhenOffscreen = true;
                skin.localBounds = new Bounds(new Vector3(0,.9f,0),new Vector3(2.3f,2.4f,2.3f));
                var glow = new GameObject("Lantern light",typeof(Light));
                glow.transform.SetParent(lantern,false); glow.transform.localPosition = new Vector3(0,-.25f,.08f);
                WarmLight(glow.GetComponent<Light>(),1.35f,3.2f);
                var animation = root.AddComponent<EmberLowPolyKeeper>();
                var view = new SerializedObject(animation);
                var names = new[] { "pelvis","head","cape","leftHip","leftKnee","leftFoot","rightHip","rightKnee","rightFoot",
                    "leftArm","rightArm","rightForearm","lantern" };
                var references = new[] { pelvis,head,cape,leftHip,leftKnee,leftFoot,rightHip,rightKnee,rightFoot,leftArm,rightArm,rightForearm,lantern };
                for (int i = 0; i < names.Length; i++) view.FindProperty(names[i]).objectReferenceValue = references[i];
                view.ApplyModifiedPropertiesWithoutUndo();
                animation.Initialize();
                return Save(root,"Keeper");
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void Lantern(EmberLowPolyMeshBuilder model, Matrix4x4 matrix, int bone)
        {
            model.Ring(matrix * Matrix4x4.Translate(new Vector3(0,-.025f,0)),.055f,.065f,.013f,(int)Paint.Gold,bone,8);
            model.Frustum(matrix * Matrix4x4.Translate(new Vector3(0,-.13f,0)),.14f,.055f,.08f,4,(int)Paint.Gold,bone);
            model.Box(matrix * Matrix4x4.Translate(new Vector3(0,-.365f,0)),new Vector3(.23f,.035f,.19f),(int)Paint.Gold,bone);
            foreach (float x in new[] { -.09f,.09f }) foreach (float z in new[] { -.065f,.065f })
                model.Box(matrix * Matrix4x4.Translate(new Vector3(x,-.25f,z)),new Vector3(.018f,.22f,.018f),(int)Paint.Iron,bone);
            model.Ellipsoid(matrix * Matrix4x4.Translate(new Vector3(0,-.26f,0)),new Vector3(.058f,.10f,.055f),(int)Paint.Flame,bone);
        }

        static GameObject Prop(string name, Action<EmberLowPolyMeshBuilder> draw, bool light = false)
        {
            var saved = Load(name);
            if (saved != null) return saved;
            var root = new GameObject(name);
            try
            {
                var mesh = new EmberLowPolyMeshBuilder(); draw(mesh);
                Renderer(root,mesh,name);
                if (light)
                {
                    var flame = new GameObject("Warm light",typeof(Light)); flame.transform.SetParent(root.transform,false);
                    flame.transform.localPosition = new Vector3(0,1.35f,0); WarmLight(flame.GetComponent<Light>(),2.2f,4.5f);
                }
                return Save(root,name);
            }
            finally { Object.DestroyImmediate(root); }
        }

        static GameObject Courtyard()
        {
            var saved = Load("Courtyard");
            if (saved != null) return saved;
            var pillar = Prop("Pillar",m => {
                Box(m,new Vector3(0,.12f,0),new Vector3(.82f,.24f,.82f),Paint.Stone);
                m.Frustum(T(new Vector3(0,1.15f,0)),.28f,.25f,1.85f,8,(int)Paint.Stone);
                Box(m,new Vector3(0,2.12f,0),new Vector3(.67f,.16f,.67f),Paint.FloorDark);
            });
            var wall = Prop("Wall",m => {
                Box(m,new Vector3(0,.18f,0),new Vector3(1.8f,.36f,.56f),Paint.Stone);
                Box(m,new Vector3(-.30f,.53f,0),new Vector3(1.20f,.34f,.49f),Paint.FloorDark);
                Box(m,new Vector3(-.53f,.81f,.01f),new Vector3(.68f,.22f,.53f),Paint.Stone);
            });
            var tree = Prop("Cypress",m => {
                m.Frustum(T(new Vector3(0,.42f,0)),.13f,.10f,.84f,6,(int)Paint.Bark);
                m.Frustum(T(new Vector3(0,1.15f,0)),.74f,.03f,1.55f,7,(int)Paint.Foliage);
                m.Frustum(T(new Vector3(0,1.92f,0)),.51f,0,1.50f,7,(int)Paint.Foliage);
            });
            var rock = Prop("Rock",m => m.Ellipsoid(T(new Vector3(0,.27f,0)),new Vector3(.54f,.40f,.42f),(int)Paint.Stone,0,7,4));
            var brazier = Prop("Brazier",m => {
                Box(m,new Vector3(0,.11f,0),new Vector3(.65f,.22f,.65f),Paint.Stone);
                m.Frustum(T(new Vector3(0,.60f,0)),.18f,.14f,.80f,6,(int)Paint.Iron);
                m.Frustum(T(new Vector3(0,1.03f,0)),.22f,.34f,.24f,6,(int)Paint.Iron);
                m.Ellipsoid(T(new Vector3(0,1.30f,0)),new Vector3(.14f,.28f,.14f),(int)Paint.Flame,0,6,4);
            },true);
            var candles = Prop("Candles",m => {
                for (int i = 0; i < 3; i++)
                {
                    float h = .22f + i * .12f; var p = new Vector3((i - 1) * .17f,h * .5f,(i % 2) * .12f);
                    m.Frustum(T(p),.06f,.055f,h,7,(int)Paint.Wax);
                    m.Ellipsoid(T(new Vector3(p.x,h + .05f,p.z)),new Vector3(.028f,.07f,.028f),(int)Paint.Flame,0,5,3);
                }
            });
            var root = new GameObject("Low-poly courtyard");
            try
            {
                var ground = new EmberLowPolyMeshBuilder();
                Box(ground,new Vector3(0,-.51f,0),new Vector3(14,.90f,16),Paint.Stone);
                Box(ground,new Vector3(0,-1.03f,0),new Vector3(90,.1f,90),Paint.Face);
                var random = new System.Random(37);
                for (int x = 0; x < 14; x++) for (int z = 0; z < 16; z++)
                    Box(ground,new Vector3(x - 6.5f,-.03f,z - 7.5f),new Vector3(.975f,.06f,.975f),
                        random.Next(4) == 0 ? Paint.FloorDark : Paint.FloorLight);
                for (int i = 0; i < 3; i++)
                    Box(ground,new Vector3(3.2f,-.15f - i * .25f,-8.4f - i * .8f),new Vector3(3.5f,.30f,.85f),Paint.Stone);
                Renderer(root,ground,"Ground");
                foreach (float x in new[] { -5.8f,5.8f }) foreach (float z in new[] { -4.6f,4.9f })
                    Place(root,pillar,new Vector3(x,0,z),Vector3.one * (z > 0 ? 1 : .75f));
                for (int i = 0; i < 5; i++) Place(root,wall,new Vector3(-5 + i * 2.3f,0,7.2f),Vector3.one);
                foreach (var point in new[] { new Vector3(-6.0f,0,-6.6f),new Vector3(6.1f,0,-6.7f),new Vector3(-6.2f,0,6),new Vector3(6.2f,0,6.2f) })
                    Place(root,tree,point,Vector3.one * .90f);
                for (int i = 0; i < 12; i++)
                {
                    float x = i % 2 == 0 ? -6.1f : 6.1f;
                    Place(root,rock,new Vector3(x,0,-5.5f + (i / 2) * 2.05f),Vector3.one * (.45f + (float)random.NextDouble() * .5f),i * 37);
                }
                Place(root,brazier,new Vector3(-4.9f,0,-3.9f),Vector3.one);
                Place(root,brazier,new Vector3(4.9f,0,4.1f),Vector3.one);
                Place(root,candles,new Vector3(-3.8f,0,-6.8f),Vector3.one);
                Place(root,candles,new Vector3(4.4f,0,6.5f),Vector3.one);
                return Save(root,"Courtyard");
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void CreateScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            var previous = SceneManager.GetActiveScene();
            // 批处理启动时可能只有未命名空场景；Unity 不允许向它叠加新场景。
            if (string.IsNullOrEmpty(previous.path))
            {
                if (Application.isBatchMode)
                    previous = EditorSceneManager.OpenScene("Assets/Scenes/Emberlight.unity");
                else
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                    previous = SceneManager.GetActiveScene();
                    if (string.IsNullOrEmpty(previous.path))
                        throw new InvalidOperationException("Save the current untitled scene before building the prototype.");
                }
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                PrefabUtility.InstantiatePrefab(Load("Courtyard"),scene);
                var keeper = (GameObject)PrefabUtility.InstantiatePrefab(Load("Keeper"),scene);
                var cameraHost = new GameObject("Low-poly camera",typeof(Camera),typeof(AudioListener));
                var camera = cameraHost.GetComponent<Camera>(); camera.tag = "MainCamera";
                camera.orthographic = true; camera.orthographicSize = 7.4f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.075f,.095f,.135f);
                camera.nearClipPlane = .1f; camera.farClipPlane = 100;
                camera.transform.position = new Vector3(8,10,-11);
                camera.transform.LookAt(Vector3.up * .65f);
                keeper.transform.rotation = Quaternion.LookRotation(new Vector3(8,0,-11));
                var moon = new GameObject("Moon",typeof(Light));
                var light = moon.GetComponent<Light>(); light.type = LightType.Directional;
                light.color = new Color(.79f,.87f,1); light.intensity = 1.1f;
                light.shadows = LightShadows.Soft; light.shadowStrength = .75f; light.shadowBias = .025f;
                moon.transform.rotation = Quaternion.Euler(48,-35,0);
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(.30f,.37f,.48f);
                RenderSettings.ambientEquatorColor = new Color(.20f,.24f,.31f);
                RenderSettings.ambientGroundColor = new Color(.11f,.13f,.17f);
                RenderSettings.fog = true; RenderSettings.fogColor = camera.backgroundColor;
                RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .012f;
                var controls = new GameObject("Low-poly preview controls",typeof(EmberLowPolyPreview));
                var view = new SerializedObject(controls.GetComponent<EmberLowPolyPreview>());
                view.FindProperty("keeper").objectReferenceValue = keeper.GetComponent<EmberLowPolyKeeper>();
                view.FindProperty("view").objectReferenceValue = camera;
                view.ApplyModifiedPropertiesWithoutUndo();
                if (!EditorSceneManager.SaveScene(scene,ScenePath)) throw new InvalidOperationException("Could not save low-poly scene.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene,true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        static void Renderer(GameObject root, EmberLowPolyMeshBuilder builder, string name)
        {
            var mesh = builder.Build(name,false,out var slots);
            root.AddComponent<MeshFilter>().sharedMesh = SaveMesh(mesh,name);
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = slots.Select(index => materials[index]).ToArray();
            renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
        }
        static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = Root + "/Meshes/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { Object.DestroyImmediate(mesh); return existing; }
            AssetDatabase.CreateAsset(mesh,path); return mesh;
        }
        static Transform Bone(string name, Transform parent, Vector3 position, List<Transform> bones)
        {
            var bone = new GameObject(name).transform; bone.SetParent(parent,false); bone.localPosition = position;
            bones.Add(bone); return bone;
        }
        static void WarmLight(Light light, float intensity, float range)
        {
            light.type = LightType.Point; light.color = new Color(1,.52f,.16f); light.intensity = intensity;
            light.range = range; light.shadows = LightShadows.None; light.renderMode = LightRenderMode.ForcePixel;
        }
        static Matrix4x4 T(Vector3 point) => Matrix4x4.Translate(point);
        static void Box(EmberLowPolyMeshBuilder builder, Vector3 point, Vector3 size, Paint paint) => builder.Box(T(point),size,(int)paint);
        static GameObject Load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + name + ".prefab");
        static GameObject Save(GameObject root, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root,Root + "/Prefabs/" + name + ".prefab",out bool saved);
            if (!saved) throw new InvalidOperationException("Could not save low-poly prefab: " + name);
            return prefab;
        }
        static void Place(GameObject root, GameObject prefab, Vector3 point, Vector3 scale, float yaw = 0)
        {
            var prop = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            prop.transform.SetParent(root.transform,false); prop.transform.localPosition = point;
            prop.transform.localScale = scale; prop.transform.localRotation = Quaternion.Euler(0,yaw,0);
        }
        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\','/'); Folder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
    }
}
