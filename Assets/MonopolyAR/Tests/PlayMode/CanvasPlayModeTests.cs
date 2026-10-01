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
            Assert.AreEqual("Jugador: Empresario\nCasilla: 00\nDinero: $1500", ui.StatusText.text);
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
            Assert.AreEqual($"Jugador: Empresario\nCasilla: {game.players[0].CurrentTile:00}\nDinero: $1500", ui.StatusText.text);
            Assert.AreEqual(game.dice.Sum, game.players[0].CurrentTile);
            CollectionAssert.AreEqual(Enumerable.Range(1, game.dice.Sum), game.players[0].LastVisited);
            Assert.IsTrue(ui.EndTurnButton.interactable);
            ui.EndTurnButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual("Jugador: Empresaria\nCasilla: 00\nDinero: $1500", ui.StatusText.text);
            Assert.AreEqual("Dado 1: -\nDado 2: -\nTotal: -", ui.ResultsText.text);
            Assert.IsTrue(ui.RollButton.interactable);
            Assert.IsFalse(ui.EndTurnButton.interactable);
        }

        [Test]
        public void PlayersHaveIndependentStartingBalances()
        {
            Assert.AreEqual(MonopolyPlayer.StartingMoney, game.players[0].Money);
            Assert.AreEqual(MonopolyPlayer.StartingMoney, game.players[1].Money);
            var moneyField = typeof(MonopolyPlayer).GetField("money", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            moneyField.SetValue(game.players[0], 900);
            Assert.AreEqual(900, game.players[0].Money);
            Assert.AreEqual(MonopolyPlayer.StartingMoney, game.players[1].Money);
            game.players[0].ResetForNewGame();
            Assert.AreEqual(MonopolyPlayer.StartingMoney, game.players[0].Money);
        }

        [UnityTest]
        public IEnumerator PassingStartPaysOnceAtThe39To0Crossing()
        {
            var player = game.players[0];
            var other = game.players[1];
            Assert.AreEqual(0, player.CurrentTile);
            Assert.AreEqual(MonopolyPlayer.StartingMoney, player.Money);

            player.PlaceAt(game.board, 38);
            yield return player.MoveSteps(game.board, 5, .01f);
            Assert.AreEqual(3, player.CurrentTile);
            Assert.AreEqual(1700, player.Money);
            CollectionAssert.AreEqual(new[] { 39, 0, 1, 2, 3 }, player.LastVisited);
            var placementMessage = ui.GetComponentInChildren<Canvas>(true).transform.Find("PlacementMessage").GetComponent<Text>();
            Assert.AreEqual("Empresario recibió $200 por pasar por SALIDA", placementMessage.text);

            yield return player.MoveSteps(game.board, 1, .01f);
            Assert.AreEqual(4, player.CurrentTile);
            Assert.AreEqual(1700, player.Money);

            player.ResetForNewGame();
            player.PlaceAt(game.board, 36);
            yield return player.MoveSteps(game.board, 4, .01f);
            Assert.AreEqual(0, player.CurrentTile);
            Assert.AreEqual(1700, player.Money);
            Assert.AreEqual(MonopolyPlayer.StartingMoney, other.Money);

            var phase = typeof(MonopolyTurnManager).GetField("<Phase>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            phase.SetValue(game, TurnPhase.AwaitEnd);
            Assert.IsTrue(game.TryEndTurn());
            Assert.AreEqual(1, game.ActivePlayerIndex);
            Assert.AreEqual(MonopolyPlayer.StartingMoney, other.Money);
        }

        [Test]
        public void BothCurrentDiceUseTheCalibratedSixFaceMapping()
        {
            var oneMesh = game.dice.dieOneVisual.GetComponentInChildren<MeshFilter>(true).sharedMesh;
            var twoMesh = game.dice.dieTwoVisual.GetComponentInChildren<MeshFilter>(true).sharedMesh;
            Assert.AreEqual(192, oneMesh.vertexCount);
            Assert.AreEqual(oneMesh.vertexCount, twoMesh.vertexCount);
            Assert.AreEqual(oneMesh.subMeshCount, twoMesh.subMeshCount);
            for (int face = 1; face <= 6; face++)
            {
                var oneUp = MonopolyDice.FaceUpRotationFor(1, face, Quaternion.identity) * MonopolyDice.FaceNormalFor(1, face);
                var twoUp = MonopolyDice.FaceUpRotationFor(2, face, Quaternion.identity) * MonopolyDice.FaceNormalFor(2, face);
                Assert.Less(Vector3.Angle(oneUp, Vector3.up), .001f, "Dado 1 cara " + face);
                Assert.Less(Vector3.Angle(twoUp, Vector3.up), .001f, "Dado 2 cara " + face);
            }
        }

        [UnityTest]
        public IEnumerator SixSixUsesTheSameVisualLogicalAndMovementValues()
        {
            var oldRandomState = Random.state;
            try
            {
                int seed = -1;
                for (int candidate = 0; candidate < 10000 && seed < 0; candidate++)
                {
                    Random.InitState(candidate);
                    if (MonopolyDice.GenerateRoll() == new Vector2Int(6, 6)) seed = candidate;
                }
                Assert.GreaterOrEqual(seed, 0, "No se encontró una semilla para 6 + 6");
                Random.InitState(seed);
                yield return game.dice.Roll();
                Assert.AreEqual(6, game.dice.DieOne);
                Assert.AreEqual(6, game.dice.DieTwo);
                Assert.AreEqual(12, game.dice.Sum);
                Assert.Less(Quaternion.Angle(game.dice.dieOneVisual.localRotation, MonopolyDice.FaceUpRotationFor(1, 6, Quaternion.identity)), .01f);
                Assert.Less(Quaternion.Angle(game.dice.dieTwoVisual.localRotation, MonopolyDice.FaceUpRotationFor(2, 6, Quaternion.identity)), .01f);
                var player = game.players[0];
                player.PlaceAt(game.board, 0);
                yield return player.MoveSteps(game.board, game.dice.Sum, .005f);
                Assert.AreEqual(12, player.CurrentTile);
                Assert.AreEqual(12, player.LastVisited.Count);
            }
            finally
            {
                Random.state = oldRandomState;
            }
        }

        [UnityTest]
        public IEnumerator RepeatedRollsKeepVisualFacesUiAndMovementInSync()
        {
            for (int round = 0; round < 4; round++)
            {
                var player = game.ActivePlayer;
                int beforeTile = player.CurrentTile;
                int beforeMoney = player.Money;
                ui.RollButton.onClick.Invoke();
                var deadline = Time.realtimeSinceStartup + 20f;
                while (!game.CanEndTurn && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(game.CanEndTurn, "La tirada no terminó");
                int sum = game.dice.Sum;
                Assert.That(game.dice.DieOne, Is.InRange(1, 6));
                Assert.That(game.dice.DieTwo, Is.InRange(1, 6));
                Assert.AreEqual(game.dice.DieOne + game.dice.DieTwo, sum);
                Assert.AreEqual($"Dado 1: {game.dice.DieOne}\nDado 2: {game.dice.DieTwo}\nTotal: {sum}", ui.ResultsText.text);
                Assert.Less(Vector3.Angle(game.dice.dieOneVisual.localRotation * MonopolyDice.FaceNormalFor(1, game.dice.DieOne), Vector3.up), .01f);
                Assert.Less(Vector3.Angle(game.dice.dieTwoVisual.localRotation * MonopolyDice.FaceNormalFor(2, game.dice.DieTwo), Vector3.up), .01f);
                int expectedTile = beforeTile;
                int crossings = 0;
                for (int step = 0; step < sum; step++)
                {
                    int next = MonopolyBoard.NextIndex(expectedTile);
                    if (expectedTile == 39 && next == 0) crossings++;
                    expectedTile = next;
                }
                Assert.AreEqual(expectedTile, player.CurrentTile);
                Assert.AreEqual(beforeMoney + crossings * MonopolyPlayer.StartBonus, player.Money);
                ui.EndTurnButton.onClick.Invoke();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CalibratedFiveFiveFiveOneAndSixSixStaySynchronized()
        {
            var oldRandomState = Random.state;
            try
            {
                var requested = new[] { new Vector2Int(5, 5), new Vector2Int(5, 1), new Vector2Int(6, 6) };
                foreach (var expectedDice in requested)
                {
                    int seed = -1;
                    for (int candidate = 0; candidate < 10000 && seed < 0; candidate++)
                    {
                        Random.InitState(candidate);
                        if (MonopolyDice.GenerateRoll() == expectedDice) seed = candidate;
                    }
                    Assert.GreaterOrEqual(seed, 0, "No se encontró semilla para " + expectedDice);

                    Random.InitState(seed);
                    var player = game.ActivePlayer;
                    int beforeTile = player.CurrentTile;
                    ui.RollButton.onClick.Invoke();
                    var deadline = Time.realtimeSinceStartup + 20f;
                    while (!game.CanEndTurn && Time.realtimeSinceStartup < deadline) yield return null;
                    Assert.IsTrue(game.CanEndTurn, "La tirada no terminó para " + expectedDice);
                    Assert.AreEqual(expectedDice.x, game.dice.DieOne);
                    Assert.AreEqual(expectedDice.y, game.dice.DieTwo);
                    Assert.AreEqual(expectedDice.x + expectedDice.y, game.dice.Sum);
                    Assert.AreEqual($"Dado 1: {expectedDice.x}\nDado 2: {expectedDice.y}\nTotal: {expectedDice.x + expectedDice.y}", ui.ResultsText.text);
                    Assert.Less(Vector3.Angle(game.dice.dieOneVisual.localRotation * MonopolyDice.FaceNormalFor(1, expectedDice.x), Vector3.up), .01f);
                    Assert.Less(Vector3.Angle(game.dice.dieTwoVisual.localRotation * MonopolyDice.FaceNormalFor(2, expectedDice.y), Vector3.up), .01f);

                    int expectedTile = beforeTile;
                    for (int step = 0; step < game.dice.Sum; step++) expectedTile = MonopolyBoard.NextIndex(expectedTile);
                    Assert.AreEqual(expectedTile, player.CurrentTile);
                    ui.EndTurnButton.onClick.Invoke();
                    yield return null;
                }
            }
            finally
            {
                Random.state = oldRandomState;
            }
        }
    }
}
#endif
