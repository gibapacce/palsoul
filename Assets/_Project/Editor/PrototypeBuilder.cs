using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Palsoul.Core;
using Palsoul.Combat;
using Palsoul.Creatures;
using Palsoul.Progression;
using Palsoul.Base;
using Palsoul.UI;
using Palsoul.Utils;
using Object = UnityEngine.Object;

namespace Palsoul.Editor
{
    /// <summary>Creates the small integration scene with serialized, editable assets.</summary>
    public static class PrototypeBuilder
    {
        public const string Root = "Assets/_Project/Prototype";
        public const string ScenePath = "Assets/_Project/Scenes/_Boot.unity";

        [InitializeOnLoadMethod]
        private static void ScheduleFirstSetup()
        {
            if (Application.isBatchMode || File.Exists(ScenePath)) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
                    || EditorSceneManager.GetActiveScene().isDirty) return;
                Build();
            };
        }

        [MenuItem("Palsoul/Create Missing Prototype Assets")]
        public static void Build()
        {
            // Existing authored scenes are never overwritten by the generator.
            if (File.Exists(ScenePath)) { Debug.Log("Prototype already exists: " + ScenePath); return; }
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory("Assets/_Project/Scenes");
            AssetDatabase.Refresh();
            // NewScene can unload assets referenced only by local variables. Create the
            // scene before loading/authoring data so prefab references remain valid.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSettings.serializationMode = SerializationMode.ForceText;
            PlayerSettings.companyName = "PalSoul";
            PlayerSettings.productName = "PalSoul Prototype";
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            settings.FindProperty("activeInputHandler").intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();
            ConfigureLayers();
            var renderer = Asset<Renderer2DData>("Renderer2D");
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, Root + "/Pipeline.asset");
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            var humanSprite = Sprite("Human", new Color32(111, 198, 235, 255), 0);
            var mossSprite = Sprite("Musguim", new Color32(139, 208, 103, 255), 1);
            var emberSprite = Sprite("Braseco", new Color32(238, 137, 78, 255), 2);
            var floorSprite = Sprite("Floor", new Color32(38, 49, 56, 255), 3);
            var anchorSprite = Sprite("Anchor", new Color32(192, 159, 240, 255), 4);
            var humanAnimator = Animator("Human", humanSprite);
            var mossAnimator = Animator("Musguim", mossSprite);
            var emberAnimator = Animator("Braseco", emberSprite);
            var movement = Asset<PlayerMovementSO>("Movement");
            var stamina = Asset<StaminaSO>("Stamina");
            var light = Attack("HumanLight", 12, .45f, 12);
            var heavy = Attack("HumanHeavy", 24, .8f, 24);
            var mossLight = Attack("MossLight", 10, .4f, 10);
            var mossHeavy = Attack("MossHeavy", 22, .75f, 22);
            var emberLight = Attack("EmberLight", 15, .55f, 15);
            var emberHeavy = Attack("EmberHeavy", 30, .95f, 28);
            var wildAttack = Attack("WildAttack", 8, .85f, 0);
            wildAttack.hitboxActiveStart = .45f;
            wildAttack.hitboxActiveEnd = .65f;
            var moss = Species("Musguim", mossSprite, mossAnimator, mossLight, mossHeavy, 70, 3.6f);
            var ember = Species("Braseco", emberSprite, emberAnimator, emberLight, emberHeavy, 90, 3.1f);
            var balance = Asset<CaptureBalanceSO>("CaptureBalance");
            var sphere = Asset<CaptureSphereDataSO>("Sphere");
            sphere.balance = balance;
            sphere.baseChance = .5f;
            sphere.icon = anchorSprite;
            var bestiary = Asset<BestiaryData>("BestiaryTemplate");
            var vigor = Asset<AttributeUpgradeSO>("Vigor");
            vigor.etherCostCurve = AnimationCurve.Linear(0, 20, 9, 200);
            var anchorData = Asset<AnchorpointDataSO>("AnchorData");
            anchorData.availableUpgrades = new[] { vigor };
            var actions = Inputs();
            var material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(material, Root + "/SpritesUnlit.mat");

            var companion = Character("Companion", mossSprite, mossAnimator, material, 9, 8);
            companion.AddComponent<SquadMemberAI>();
            Set(companion.GetComponent<SquadMemberAI>(), "enemyLayer", 1 << 8);
            var companionPrefab = Prefab(companion, "Companion");
            var mossPrefab = Wild(moss, wildAttack, material);
            var emberPrefab = Wild(ember, wildAttack, material);
            var player = Character("Player", humanSprite, humanAnimator, material, 9, 8);
            player.tag = "Player";
            player.AddComponent<StaminaSystem>();
            Set(player.GetComponent<StaminaSystem>(), "staminaData", stamina);
            var controller = player.AddComponent<PlayerController>();
            Set(controller, "movementData", movement);
            Set(controller, "staminaData", stamina);
            Set(controller, "lightAttackData", light);
            Set(controller, "heavyAttackData", heavy);
            var input = player.AddComponent<PlayerInput>();
            input.actions = actions;
            input.defaultActionMap = "Player";
            input.notificationBehavior = PlayerNotifications.SendMessages;
            player.AddComponent<EtherWallet>();
            // Starting funds isolate the MVP 7 upgrade test from the future loot system.
            Set(player.GetComponent<EtherWallet>(), "startingEther", 100f);
            player.AddComponent<PlayerProgression>();
            player.AddComponent<TransformationSystem>();
            player.AddComponent<SquadController>();
            Set(player.GetComponent<SquadController>(), "squadMemberPrefab", companionPrefab);
            var capture = player.AddComponent<CaptureSystem>();
            Set(capture, "activeSphere", sphere);
            Set(capture, "bestiary", bestiary);
            Set(capture, "sphereCount", 12);
            Set(capture, "captureRadius", 2.5f);
            Set(capture, "creatureLayer", 1 << 8);
            var projectile = new GameObject("CaptureSphere", typeof(SpriteRenderer));
            projectile.transform.SetParent(player.transform, false);
            projectile.transform.localScale = Vector3.one * .25f;
            projectile.GetComponent<SpriteRenderer>().sharedMaterial = material;
            projectile.GetComponent<SpriteRenderer>().sortingOrder = 5;
            Set(capture, "projectileView", projectile.GetComponent<SpriteRenderer>());
            projectile.SetActive(false);
            PrefabUtility.SaveAsPrefabAssetAndConnect(player, Root + "/Player.prefab", InteractionMode.AutomatedAction);
            player.transform.position = new Vector3(-5, 0);
            var hud = new GameObject("HUD").AddComponent<PrototypeHUD>();
            Set(hud, "player", controller);

            var cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow));
            cameraGO.tag = "MainCamera";
            cameraGO.transform.position = new Vector3(-5, 0, -10);
            var camera = cameraGO.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.625f;
            camera.backgroundColor = new Color(.06f, .09f, .12f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraGO.GetComponent<CameraFollow>().SetTarget(player.transform);
            var pixel = cameraGO.AddComponent<PixelPerfectCamera>();
            pixel.assetsPPU = 16;
            pixel.refResolutionX = 320;
            pixel.refResolutionY = 180;
            pixel.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            pixel.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
            cameraGO.AddComponent<UniversalAdditionalCameraData>();

            var anchor = new GameObject("Ancoradouro", typeof(SpriteRenderer), typeof(AnchorpointMenuUI), typeof(AnchorpointController));
            anchor.transform.position = new Vector3(-6, 1);
            anchor.GetComponent<SpriteRenderer>().sprite = anchorSprite;
            anchor.GetComponent<SpriteRenderer>().sharedMaterial = material;
            Set(anchor.GetComponent<AnchorpointController>(), "data", anchorData);
            PrefabUtility.SaveAsPrefabAssetAndConnect(anchor, Root + "/Anchor.prefab", InteractionMode.AutomatedAction);
            var world = new GameObject("World").AddComponent<WorldResetSystem>();
            world.RegisterSpawn(mossPrefab, new Vector3(1, 2), Quaternion.identity);
            world.RegisterSpawn(emberPrefab, new Vector3(6, -2), Quaternion.identity);
            world.RegisterSpawn(mossPrefab, new Vector3(9, 3), Quaternion.identity);
            for (int x = -12; x <= 14; x++) for (int y = -7; y <= 7; y++)
            {
                var tile = new GameObject($"Floor_{x}_{y}", typeof(SpriteRenderer));
                tile.transform.position = new Vector3(x, y, 0);
                var sr = tile.GetComponent<SpriteRenderer>();
                sr.sprite = floorSprite;
                sr.sharedMaterial = material;
                sr.sortingOrder = -10;
                if (x == -12 || x == 14 || y == -7 || y == 7)
                {
                    tile.AddComponent<BoxCollider2D>();
                    sr.color = new Color(.5f, .6f, .7f);
                }
            }
            foreach (var asset in AssetDatabase.FindAssets("", new[] { Root }))
            {
                var obj = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(asset));
                if (obj != null) EditorUtility.SetDirty(obj);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("PalSoul prototype generated. Open _Boot and press Play.");
        }

        private static T Asset<T>(string name) where T : ScriptableObject
        {
            string path = Root + "/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
        public static void Set(Object target, string name, object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(name);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + "." + name);
            if (value is Object obj)
            {
                if (obj == null) throw new InvalidOperationException("Unloaded asset for " + target.name + "." + name);
                property.objectReferenceValue = obj;
            }
            else if (value is int integer) property.intValue = integer;
            else if (value is float number) property.floatValue = number;
            else if (value is bool boolean) property.boolValue = boolean;
            else throw new ArgumentException(name);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void ConfigureLayers()
        {
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            layers.GetArrayElementAtIndex(8).stringValue = "Enemy";
            layers.GetArrayElementAtIndex(9).stringValue = "Friendly";
            tags.ApplyModifiedPropertiesWithoutUndo();
            Physics2D.IgnoreLayerCollision(9, 9);
        }
        private static AttackDataSO Attack(string name, float damage, float duration, float stamina)
        {
            var data = Asset<AttackDataSO>(name);
            data.attackName = name;
            data.baseDamage = damage;
            data.duration = duration;
            data.staminaCost = stamina;
            data.hitboxSize = new Vector2(1.1f, 1f);
            return data;
        }
        private static CreatureDefinitionSO Species(string name, Sprite sprite, RuntimeAnimatorController animator,
            AttackDataSO light, AttackDataSO heavy, float hp, float speed)
        {
            var species = Asset<CreatureDefinitionSO>(name);
            species.creatureName = name;
            species.worldSprite = species.portrait = sprite;
            species.animatorController = animator;
            species.lightAttack = light;
            species.heavyAttack = heavy;
            species.baseHP = hp;
            species.baseMoveSpeed = speed;
            species.workAffinity = WorkAffinity.Mining;
            return species;
        }
        private static GameObject Character(string name, Sprite sprite, RuntimeAnimatorController animator,
            Material material, int layer, int targetLayer)
        {
            var go = new GameObject(name, typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CapsuleCollider2D),
                typeof(Animator), typeof(HealthSystem), typeof(HitboxController), typeof(HurtboxController), typeof(CombatFeedback));
            go.layer = layer;
            go.GetComponent<SpriteRenderer>().sprite = sprite;
            go.GetComponent<SpriteRenderer>().sharedMaterial = material;
            go.GetComponent<Animator>().runtimeAnimatorController = animator;
            var rb = go.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            go.GetComponent<CapsuleCollider2D>().size = new Vector2(.65f, .7f);
            Set(go.GetComponent<HitboxController>(), "hurtboxLayer", 1 << targetLayer);
            return go;
        }
        private static GameObject Wild(CreatureDefinitionSO species, AttackDataSO attack, Material material)
        {
            var data = Asset<EnemyDataSO>(species.creatureName + "Enemy");
            data.enemyName = species.creatureName;
            data.maxHP = species.baseHP;
            data.attackData = attack;
            data.detectionRadius = 3;
            data.loseAggroRadius = 5;
            data.patrolRadius = 1;
            data.chaseSpeed = 2;
            var go = Character(species.creatureName, species.worldSprite, species.animatorController, material, 8, 9);
            Set(go.GetComponent<HealthSystem>(), "maxHP", species.baseHP);
            Set(go.AddComponent<EnemyController>(), "enemyData", data);
            Set(go.AddComponent<CreatureController>(), "_definition", species);
            return Prefab(go, species.creatureName);
        }
        private static GameObject Prefab(GameObject go, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, Root + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }
        private static Sprite Sprite(string name, Color32 color, int shape)
        {
            var texture = new Texture2D(16, 16);
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
            {
                bool filled = shape == 3 || (x >= 3 && x <= 12 && y >= 2 && y <= 12);
                if (shape == 1) filled = (x - 8) * (x - 8) + (y - 7) * (y - 7) < 49;
                if (shape == 2) filled = Math.Abs(x - 8) + Math.Abs(y - 8) < 9;
                Color32 pixel = filled ? color : new Color32(0, 0, 0, 0);
                if (shape != 3 && y == 9 && (x == 6 || x == 10)) pixel = new Color32(20, 24, 30, 255);
                if (shape == 3 && (x == 0 || y == 0)) pixel = new Color32(45, 57, 64, 255);
                texture.SetPixel(x, y, pixel);
            }
            texture.Apply();
            string path = Root + "/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static AnimatorController Animator(string name, Sprite sprite)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(Root + "/" + name + ".controller");
            foreach (string parameter in new[] { "IsMoving", "IsDodging", "IsChasing", "IsAttacking", "IsStaggered", "IsDead" })
                controller.AddParameter(parameter, AnimatorControllerParameterType.Bool);
            foreach (string parameter in new[] { "MoveX", "MoveY", "AttackSpeed" })
                controller.AddParameter(parameter, AnimatorControllerParameterType.Float);
            foreach (string parameter in new[] { "AttackLight", "AttackHeavy", "Death" })
                controller.AddParameter(parameter, AnimatorControllerParameterType.Trigger);
            var clip = new AnimationClip();
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
                new[] { new ObjectReferenceKeyframe { time = 0, value = sprite } });
            AssetDatabase.CreateAsset(clip, Root + "/" + name + "Idle.anim");
            var machine = controller.layers[0].stateMachine;
            var idle = machine.AddState("Idle");
            idle.motion = clip;
            var walk = machine.AddState("Walk");
            walk.motion = clip;
            var toWalk = idle.AddTransition(walk);
            toWalk.hasExitTime = false;
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            var toIdle = walk.AddTransition(idle);
            toIdle.hasExitTime = false;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            return controller;
        }
        private static InputActionAsset Inputs()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = asset.AddActionMap("Player");
            var move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            string[] names = { "AttackLight", "AttackHeavy", "Dodge", "Capture", "Swap" };
            string[] keys = { "j", "k", "space", "q", "tab" };
            string[] pads = { "rightShoulder", "rightTrigger", "buttonEast", "buttonNorth", "leftShoulder" };
            for (int i = 0; i < names.Length; i++)
            {
                var action = map.AddAction(names[i], InputActionType.Button);
                action.AddBinding("<Keyboard>/" + keys[i]);
                action.AddBinding("<Gamepad>/" + pads[i]);
            }
            string path = Root + "/Player.inputactions";
            File.WriteAllText(path, asset.ToJson());
            Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }
    }
}
