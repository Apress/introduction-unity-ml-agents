using Unity.MLAgents.Editor;
using UnityEditor;
using Unity.MLAgents.Sensors;
namespace Ecosystem
{
    [CustomEditor(typeof(EcosystemRenderTextureSensorComponent), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    internal class EcosystemRenderTextureSensorComponentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            // Drawing the RenderTextureComponent
            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginDisabledGroup(!EditorUtilities.CanUpdateModelProperties());
            {
                EditorGUILayout.PropertyField(so.FindProperty("m_RenderTexture"), true);
                EditorGUILayout.PropertyField(so.FindProperty("m_SensorName"), true);
                EditorGUILayout.PropertyField(so.FindProperty("m_Grayscale"), true);
                EditorGUILayout.PropertyField(so.FindProperty("m_ObservationStacks"), true);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(so.FindProperty("m_Compression"), true);

            var requireSensorUpdate = EditorGUI.EndChangeCheck();
            so.ApplyModifiedProperties();

            if (requireSensorUpdate)
            {
                UpdateSensor();
            }
        }

        void UpdateSensor()
        {
            var sensorComponent = serializedObject.targetObject as EcosystemRenderTextureSensorComponent;
            if (sensorComponent != null)
            {
                sensorComponent.UpdateSensor();
            }
        }
    }
}
