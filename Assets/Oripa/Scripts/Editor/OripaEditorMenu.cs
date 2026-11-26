using System.Linq;
using System.Text;
using Oripa.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Oripa.EditorTools
{
    public static class OripaEditorMenu
    {
        const string SceneFolder = "Assets/Oripa/Scenes";
        const string ScenePath = SceneFolder + "/GachaPrototype.unity";

        [MenuItem("Oripa/프로토타입 씬 만들기", priority = 0)]
        public static void CreatePrototypeScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!AssetDatabase.IsValidFolder(SceneFolder))
                AssetDatabase.CreateFolder("Assets/Oripa", "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.933f, 0.945f, 0.969f);
            cam.orthographic = true;

            new GameObject("GachaApp").AddComponent<GachaApp>();

            EditorSceneManager.SaveScene(scene, ScenePath);

            var list = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();

            Debug.Log($"[Oripa] {ScenePath} 를 만들고 빌드 설정 첫 번째 씬으로 등록했습니다. Scene 뷰에서 바로 UI가 보이고, Play를 누르면 동작합니다. " +
                      "Game 뷰 해상도는 1080x1920 (세로)을 권장합니다.");
        }

        [MenuItem("Oripa/씬 뷰 미리보기 새로고침", priority = 10)]
        public static void RefreshPreview()
        {
            var apps = Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
                .Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt)
                .Where(s => s.isLoaded)
                .SelectMany(s => s.GetRootGameObjects())
                .SelectMany(go => go.GetComponentsInChildren<GachaApp>())
                .ToList();
            if (apps.Count == 0)
            {
                Debug.LogWarning("[Oripa] 열린 씬에 GachaApp이 없습니다. 먼저 'Oripa > 프로토타입 씬 만들기'를 실행하세요.");
                return;
            }
            foreach (var app in apps) app.RefreshPreview();
            Debug.Log("[Oripa] 미리보기를 다시 만들었습니다. Scene 뷰에서 2D 모드를 켜고 GachaCanvas를 더블클릭하면 화면 전체가 보입니다.");
        }

        [MenuItem("Oripa/씬 뷰 미리보기 새로고침", true)]
        static bool RefreshPreviewValidate() => !EditorApplication.isPlaying;

        // ───────────────────────── UI 굽기 (Scene 뷰에서 직접 편집)

        const string RoundedAssetPath = "Assets/Oripa/Resources/" + UIKit.RoundedResourcePath + ".png";

        [MenuItem("Oripa/UI를 씬에 굽기 (직접 편집)", priority = 11)]
        public static void BakeUI()
        {
            var app = FindApp();
            if (app == null) return;

            if (app.IsBaked && app.BakedCanvas != null &&
                !EditorUtility.DisplayDialog("UI를 다시 굽기",
                    "이미 구운 UI가 있어요. 지우고 처음 모양으로 다시 만들까요?\n(Scene에서 고친 위치, 색, 글자는 사라져요. Ctrl+Z로 되돌릴 수 있어요.)",
                    "다시 굽기", "취소"))
                return;

            EnsureRoundedSprite();

            Undo.RegisterCompleteObjectUndo(app, "Oripa UI 굽기");
            if (app.BakedCanvas != null) Undo.DestroyObjectImmediate(app.BakedCanvas.gameObject);
            app.ClearBakedReferences();

            var canvas = app.BakeLayout();
            Undo.RegisterCreatedObjectUndo(canvas, "Oripa UI 굽기");
            EditorUtility.SetDirty(app);
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            app.RefreshPreview();
            Selection.activeGameObject = canvas;

            Debug.Log("[Oripa] UI를 씬에 구웠습니다. GachaApp > GachaCanvas 아래 오브젝트를 Scene 뷰에서 직접 옮기고 고칠 수 있어요. " +
                      "팩 목록과 보유 카드는 PackListSlot / CollectionSlot 안에 실행할 때 채워집니다. 씬을 저장(Ctrl+S)하세요.");
        }

        [MenuItem("Oripa/UI를 씬에 굽기 (직접 편집)", true)]
        static bool BakeUIValidate() => !EditorApplication.isPlaying;

        [MenuItem("Oripa/구운 UI에 코인 표시 추가", priority = 13)]
        public static void AddCurrency()
        {
            var app = FindApp();
            if (app == null) return;
            if (!app.IsBaked)
            {
                Debug.LogWarning("[Oripa] 구운 UI가 없습니다. 코드 생성 방식에는 코인 표시가 이미 들어 있어요.");
                return;
            }
            Undo.RegisterCompleteObjectUndo(app, "Oripa 코인 표시 추가");
            var row = app.AddCurrencyToBakedHeader();
            if (row == null)
            {
                Debug.LogWarning("[Oripa] Header를 찾지 못했거나 이미 Currency가 있습니다.");
                return;
            }
            Undo.RegisterCreatedObjectUndo(row, "Oripa 코인 표시 추가");
            EditorUtility.SetDirty(app);
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            app.RefreshPreview();
            Selection.activeGameObject = row;
            Debug.Log("[Oripa] Header 오른쪽 위에 코인 표시를 추가했습니다. 위치를 조정하고 씬을 저장(Ctrl+S)하세요.");
        }

        [MenuItem("Oripa/구운 UI: 가로 스크롤 배너 넣기", priority = 14)]
        public static void InstallBanner()
        {
            var app = FindApp();
            if (app == null) return;
            if (!app.IsBaked)
            {
                Debug.LogWarning("[Oripa] 구운 UI가 없습니다. 코드 생성 방식에는 이미 가로 스크롤 배너가 들어 있어요.");
                return;
            }
            Undo.RegisterCompleteObjectUndo(app, "Oripa 가로 스크롤 배너");
            var banner = app.InstallBannerCarousel(out var old);
            if (banner == null)
            {
                Debug.LogWarning("[Oripa] 이미 가로 스크롤 배너가 있거나, 바꿀 BalanceRow / Banner를 찾지 못했습니다.");
                return;
            }
            Undo.RegisterCreatedObjectUndo(banner, "Oripa 가로 스크롤 배너");
            if (old.name == "BalanceRow") app.ForgetBalanceReferences();
            Undo.DestroyObjectImmediate(old);
            EditorUtility.SetDirty(app);
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            app.RefreshPreview();
            Selection.activeGameObject = banner;
            var size = GachaApp.BannerSize;
            Debug.Log($"[Oripa] 가로 스크롤 배너를 넣었습니다 (페이지 {size.x:0} x {size.y:0}). " +
                      "Banner > Pages > Page1~3 의 Source Image에 그림(Sprite)을 넣고 PageHint 글자는 지우세요. " +
                      "페이지를 늘리려면 Page를 복제(Ctrl+D)하면 됩니다. 씬을 저장(Ctrl+S)하세요.");
        }

        [MenuItem("Oripa/구운 UI: 배너 양옆 미리보기 + 구분선 + 꽉 찬 안내", priority = 15)]
        public static void UpgradeBannerArea()
        {
            var app = FindApp();
            if (app == null) return;
            if (!app.IsBaked || app.BakedCanvasRoot == null)
            {
                Debug.LogWarning("[Oripa] 구운 UI가 없습니다. 코드 생성 방식에는 이미 들어 있어요.");
                return;
            }
            Undo.RegisterFullObjectHierarchyUndo(app.BakedCanvasRoot.gameObject, "Oripa 배너 영역 정리");
            if (!app.UpgradeBannerArea(out var dividers))
            {
                Debug.LogWarning("[Oripa] 가로 스크롤 배너가 없습니다. 먼저 'Oripa > 구운 UI: 가로 스크롤 배너 넣기'를 실행하세요.");
                return;
            }
            if (dividers != null) Undo.RegisterCreatedObjectUndo(dividers, "Oripa 배너 영역 정리");
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            app.RefreshPreview();
            var size = GachaApp.BannerSize;
            Debug.Log($"[Oripa] 배너 양옆에 이전/다음 페이지가 보이게 하고, 구분선 두 줄을 넣고, 안내 상자를 화면 양 끝까지 붙였습니다. " +
                      $"배너 그림 크기: {size.x:0} x {size.y:0}. 씬을 저장(Ctrl+S)하세요.");
        }

        [MenuItem("Oripa/테스트: 포인트 1,000,000 충전 (Play 중)", priority = 30)]
        public static void ChargeInPlay()
        {
            var app = Object.FindAnyObjectByType<GachaApp>();
            if (app != null) app.Charge();
        }

        [MenuItem("Oripa/테스트: 포인트 1,000,000 충전 (Play 중)", true)]
        static bool ChargeInPlayValidate() => EditorApplication.isPlaying;

        [MenuItem("Oripa/구운 UI 지우기 (코드 생성으로 되돌리기)", priority = 12)]
        public static void UnbakeUI()
        {
            var app = FindApp();
            if (app == null || !app.IsBaked) return;
            if (!EditorUtility.DisplayDialog("구운 UI 지우기",
                    "씬에 저장된 UI를 지우고 코드로 만드는 방식으로 돌아갈까요? (Ctrl+Z로 되돌릴 수 있어요.)", "지우기", "취소"))
                return;

            Undo.RegisterCompleteObjectUndo(app, "Oripa 구운 UI 지우기");
            if (app.BakedCanvas != null) Undo.DestroyObjectImmediate(app.BakedCanvas.gameObject);
            app.ClearBakedReferences();
            EditorUtility.SetDirty(app);
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            app.RefreshPreview();
        }

        [MenuItem("Oripa/구운 UI 지우기 (코드 생성으로 되돌리기)", true)]
        static bool UnbakeUIValidate()
        {
            if (EditorApplication.isPlaying) return false;
            var app = FindAppQuiet();
            return app != null && app.IsBaked;
        }

        /// <summary>둥근 모서리 스프라이트를 에셋으로 만들어 둔다. 씬에 저장된 Image가 이 에셋을 참조한다.</summary>
        static void EnsureRoundedSprite()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(RoundedAssetPath) != null) return;

            var dir = System.IO.Path.GetDirectoryName(RoundedAssetPath);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);

            var tex = UIKit.MakeRoundedTexture(UIKit.RoundedTextureSize, UIKit.RoundedBorder, false);
            System.IO.File.WriteAllBytes(RoundedAssetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(RoundedAssetPath, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(RoundedAssetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = new Vector4(UIKit.RoundedBorder, UIKit.RoundedBorder, UIKit.RoundedBorder, UIKit.RoundedBorder);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            UIKit.ResetSpriteCache();
            Debug.Log("[Oripa] 둥근 모서리 스프라이트를 만들었습니다: " + RoundedAssetPath);
        }

        static GachaApp FindApp()
        {
            var app = FindAppQuiet();
            if (app == null)
                Debug.LogWarning("[Oripa] 열린 씬에 GachaApp이 없습니다. 먼저 'Oripa > 프로토타입 씬 만들기'를 실행하세요.");
            return app;
        }

        static GachaApp FindAppQuiet()
        {
            return Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
                .Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt)
                .Where(s => s.isLoaded)
                .SelectMany(s => s.GetRootGameObjects())
                .SelectMany(go => go.GetComponentsInChildren<GachaApp>())
                .FirstOrDefault();
        }

        [MenuItem("Oripa/확률 시뮬레이션", priority = 20)]
        public static void SimulateOdds()
        {
            const int runs = 100_000;
            var catalog = GachaCatalog.CreateDefault();
            var engine = new GachaEngine(new System.Random(12345));
            var sb = new StringBuilder();

            foreach (var pack in catalog.Packs)
            {
                var state = new PlayerState();
                sb.AppendLine($"■ {pack.Name}  (1회 {pack.Price:N0}pt, 환원율 {GachaEngine.Rtp(pack, state):0.0}%)");

                if (pack.Mode == PackMode.Odds)
                {
                    // 확률 방식: 수량 제한 없이 10만 번 등급만 뽑아서 확률표와 비교
                    var counts = new int[GradeInfo.Count];
                    var ps = state.GetPack(pack);
                    for (int i = 0; i < runs; i++) counts[(int)engine.Roll(pack, ps)]++;
                    foreach (var g in GradeInfo.DisplayOrder.Where(pack.HasGrade))
                        sb.AppendLine($"   {GradeInfo.Label(g)}  기대 {pack.Odds[(int)g],6:0.00}%   실측 {counts[(int)g] * 100.0 / runs,6:0.00}%");
                }
                else
                {
                    // 재고 방식: 전량을 뽑으면 초기 재고와 정확히 같아야 한다
                    state.points = long.MaxValue / 2;
                    var got = new int[GradeInfo.Count];
                    while (engine.TryDraw(pack, state, 1, out var results) == DrawStatus.Success)
                        foreach (var r in results) got[(int)r.grade]++;
                    bool ok = got.SequenceEqual(pack.Stock);
                    sb.AppendLine($"   전량 {pack.Total}장 소진: {(ok ? "재고와 일치 ✔" : "불일치 ✘")}");
                    foreach (var g in GradeInfo.DisplayOrder.Where(pack.HasGrade))
                        sb.AppendLine($"   {GradeInfo.Label(g)}  {got[(int)g],5} / {pack.Stock[(int)g]}");
                }
            }

            Debug.Log("[Oripa] 확률 시뮬레이션 결과\n" + sb);
        }

        [MenuItem("Oripa/저장 데이터 삭제", priority = 40)]
        public static void DeleteSave()
        {
            if (EditorUtility.DisplayDialog("저장 데이터 삭제", "PlayerPrefs의 뽑기 저장 데이터를 삭제할까요?", "삭제", "취소"))
            {
                SaveSystem.Delete();
                Debug.Log("[Oripa] 저장 데이터를 삭제했습니다.");
            }
        }
    }
}
