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
