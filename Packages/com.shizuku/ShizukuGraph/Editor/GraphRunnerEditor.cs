using System.Linq;
using Shizuku.Graph;
using UnityEditor;
using UnityEngine;

namespace Shizuku.Graph.Editor
{
    [CustomEditor(typeof(GraphRunner))]
    public sealed class GraphRunnerEditor : UnityEditor.Editor
    {
        private SerializedProperty _graphAssetProperty;

        private void OnEnable()
        {
            _graphAssetProperty = serializedObject.FindProperty(nameof(GraphRunner.GraphAsset));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_graphAssetProperty);
            var graphChanged = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            if (graphChanged)
            {
                foreach (var inspectedTarget in targets)
                {
                    if (inspectedTarget is not GraphRunner inspectedRunner)
                        continue;

                    Undo.RecordObject(inspectedRunner, "修改 GraphRunner 图资产");
                    inspectedRunner.RemoveInvalidSceneVariableBindings();
                    EditorUtility.SetDirty(inspectedRunner);
                }
            }

            if (targets.Length != 1)
            {
                EditorGUILayout.HelpBox("多对象编辑时不显示场景变量绑定。", MessageType.Info);
                return;
            }

            var runner = (GraphRunner)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("场景变量绑定", EditorStyles.boldLabel);

            if (runner.GraphAsset == null)
            {
                EditorGUILayout.HelpBox("先指定 Graph Asset。", MessageType.Info);
                return;
            }

            var sceneVariables = runner.GraphAsset.Variables
                .Where(variable => variable != null &&
                    (variable.Type == VariableType.GameObject || variable.Type == VariableType.Transform))
                .ToList();

            if (sceneVariables.Count == 0)
            {
                EditorGUILayout.HelpBox("当前图没有 GameObject 或 Transform 变量。", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                "场景对象不能可靠保存在图资产默认值中。这里的绑定属于当前 GraphRunner 实例，并会在图首次执行前覆盖变量默认值。",
                MessageType.None);

            foreach (var variable in sceneVariables)
            {
                runner.TryGetSceneVariableBinding(variable.GUID, out var currentValue);
                var objectType = variable.Type == VariableType.GameObject
                    ? typeof(GameObject)
                    : typeof(Transform);

                EditorGUI.BeginChangeCheck();
                var nextValue = EditorGUILayout.ObjectField(
                    new GUIContent(variable.Name, $"{variable.Type} · {variable.GUID}"),
                    currentValue,
                    objectType,
                    true);

                if (!EditorGUI.EndChangeCheck())
                    continue;

                Undo.RecordObject(runner, $"绑定场景变量 {variable.Name}");
                if (runner.SetSceneVariableBinding(variable.GUID, nextValue))
                    EditorUtility.SetDirty(runner);
            }
        }
    }
}
