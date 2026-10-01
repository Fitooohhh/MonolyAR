#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MonopolyAR;

public sealed class MonopolyDiceCalibrationWindow : EditorWindow
{
    [MenuItem("Monopoly AR/Dice Calibration")]
    private static void Open()
    {
        GetWindow<MonopolyDiceCalibrationWindow>("Dice Calibration");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Modo de diagnóstico: coloca las caras directamente, sin random, animación ni movimiento. " +
            "Comprueba la cara superior desde la vista de Unity.", MessageType.Info);

        MonopolyDice dice = FindObjectOfType<MonopolyDice>();
        if (!dice)
        {
            EditorGUILayout.HelpBox("No se encontró MonopolyDice en la escena activa.", MessageType.Warning);
            return;
        }

        DrawDieButtons(dice, 1, "Dado 1");
        EditorGUILayout.Space(8f);
        DrawDieButtons(dice, 2, "Dado 2");
        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Restaurar orientación inicial"))
        {
            Undo.RecordObjects(new Object[] { dice.dieOneVisual, dice.dieTwoVisual }, "Restore dice calibration pose");
            dice.dieOneVisual.localRotation = Quaternion.identity;
            dice.dieTwoVisual.localRotation = Quaternion.identity;
            EditorSceneManager.MarkSceneDirty(dice.gameObject.scene);
        }
    }

    private static void DrawDieButtons(MonopolyDice dice, int dieIndex, string label)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        try
        {
            for (int face = 1; face <= 6; face++)
            {
                int selectedFace = face;
                if (GUILayout.Button(selectedFace.ToString()))
                {
                    Transform target = dieIndex == 1 ? dice.dieOneVisual : dice.dieTwoVisual;
                    Undo.RecordObject(target, "Calibrate die face");
                    dice.SetCalibrationFace(dieIndex, selectedFace);
                    EditorSceneManager.MarkSceneDirty(dice.gameObject.scene);
                }
            }
        }
        finally
        {
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
