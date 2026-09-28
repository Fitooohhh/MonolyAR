#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

namespace MonopolyAR.Tests
{
    public class CanvasPlayModeTests
    {
        private MonopolyPrototypeUI ui;
        private MonopolyTurnManager game;

        [UnitySetUp]
        public IEnumerator OpenScene()
        {
            yield return ARTestPlacement.ResetXR();
            EditorSceneManager.LoadSceneInPlayMode("Assets/MonopolyAR/Scenes/MonopolyAR_Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            ui = Object.FindObjectOfType<MonopolyPrototypeUI>();
            game = Object.FindObjectOfType<MonopolyTurnManager>();
            Assert.NotNull(ui);
            yield return ARTestPlacement.PlaceDefault();
            Assert.AreEqual(TurnPhase.AwaitRoll, game.Phase);
        }

        [UnityTest]
        public IEnumerator InitialTextsAndCheckboxControlThe40Numbers()
        {
            Assert.AreEqual("Jugador: Empresario\nCasilla: 00", ui.StatusText.text);
            Assert.AreEqual("Dado 1: -\nDado 2: -\nTotal: -", ui.ResultsText.text);
            var canvas = ui.GetComponentInChildren<Canvas>();
            var panel = canvas.transform.Find("SafeArea/StatusPanel");
            var layer = canvas.transform.Find("TileNumbers").gameObject;
            Assert.AreEqual(1, panel.parent.childCount, "Only one control panel");
            Assert.AreEqual(40, layer.transform.childCount);
            Assert.IsEmpty(panel.GetComponentsInChildren<Shadow>(true));
            Assert.AreEqual(ui.RollButton.image.color, ui.EndTurnButton.image.color);
            Assert.AreEqual(ui.RollButton.colors, ui.EndTurnButton.colors);
            Assert.IsNull(ui.NumbersToggle.GetComponent<Button>());
            Assert.AreEqual("Mostrar números", ui.NumbersToggle.transform.Find("Label").GetComponent<Text>().text);
            Assert.IsTrue(ui.ShowTileNumbers);
            // Dispatch the same UI pointer event used by the mouse or Android touch.
            var click = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(ui.NumbersToggle.gameObject, click, ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.IsFalse(ui.NumbersToggle.isOn);
            Assert.IsFalse(ui.ShowTileNumbers);
            Assert.IsFalse(layer.activeSelf);
            ExecuteEvents.Execute(ui.NumbersToggle.gameObject, click, ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.IsTrue(ui.NumbersToggle.isOn);
            Assert.IsTrue(ui.ShowTileNumbers);
            Assert.IsTrue(layer.activeSelf);
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
            Assert.AreEqual(new Vector2(1280, 800), scaler.referenceResolution);
            Assert.AreEqual(.5f, scaler.matchWidthOrHeight);
        }

        [UnityTest]
        public IEnumerator RollDisplaysRealResultsAndSwitchesPlayerWithCorrectText()
        {
            ui.RollButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(ui.RollButton.interactable);
            Assert.IsFalse(ui.EndTurnButton.interactable);
            var deadline = Time.realtimeSinceStartup + 15;
            bool moved = false;
            while (!game.CanEndTurn && Time.realtimeSinceStartup < deadline)
            {
                if (game.Phase == TurnPhase.Moving)
                {
                    moved = true;
                    Assert.IsFalse(ui.RollButton.interactable);
                    Assert.IsFalse(ui.EndTurnButton.interactable);
                }
                yield return null;
            }
            Assert.IsTrue(game.CanEndTurn);
            Assert.IsTrue(moved);
            yield return null;
            Assert.AreEqual($"Dado 1: {game.dice.DieOne}\nDado 2: {game.dice.DieTwo}\nTotal: {game.dice.Sum}", ui.ResultsText.text);
            Assert.AreEqual($"Jugador: Empresario\nCasilla: {game.players[0].CurrentTile:00}", ui.StatusText.text);
            Assert.AreEqual(game.dice.Sum, game.players[0].CurrentTile);
            CollectionAssert.AreEqual(Enumerable.Range(1, game.dice.Sum), game.players[0].LastVisited);
            Assert.IsTrue(ui.EndTurnButton.interactable);
            ui.EndTurnButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual("Jugador: Empresaria\nCasilla: 00", ui.StatusText.text);
            Assert.AreEqual("Dado 1: -\nDado 2: -\nTotal: -", ui.ResultsText.text);
            Assert.IsTrue(ui.RollButton.interactable);
            Assert.IsFalse(ui.EndTurnButton.interactable);
        }
    }
}
#endif
