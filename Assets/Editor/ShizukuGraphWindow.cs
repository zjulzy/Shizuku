using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class ShizukuGraphWindow : EditorWindow
{
    private ShizukuGraphView _graphView;
    
    [MenuItem("Shizuku/ShizukuGraphWindow")]
    public static void OpenWindow()
    {
        ShizukuGraphWindow window = GetWindow<ShizukuGraphWindow>();
        window.titleContent = new GUIContent("Shizuku Graph");
    }

    private void OnEnable()
    {
        _graphView = new ShizukuGraphView();
        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
        
        // 工具栏
        Toolbar toolbar = new Toolbar();
        Button nodeCreateButton = new Button(() => { Debug.Log("测试按钮"); });
        nodeCreateButton.text = "测试按钮";
        Button saveButton = new Button(() => {_graphView.SaveToAsset();});
        saveButton.text = "保存";
        
        toolbar.Add(nodeCreateButton);
        toolbar.Add(saveButton);
        rootVisualElement.Add(toolbar);
    }
    
    private void OnDisable()
    {
        rootVisualElement.Clear();
        _graphView = null;
    }
    
    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        ShizukuGraphBase graphAsset = EditorUtility.InstanceIDToObject(instanceID) as ShizukuGraphBase;
        if (graphAsset != null)
        {
            ShizukuGraphWindow window = GetWindow<ShizukuGraphWindow>();
            window.titleContent = new GUIContent("Shizuku Graph");
            
            window._graphView.LoadFromAsset(graphAsset);
            window.Show();
            return true;
        }
        return false;
    }
}
