#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

namespace MonopolyAR.Tests
{
    public class DynamicPlayersPlayModeTests
    {
        private MonopolyTurnManager game;
        private MonopolyPrototypeUI ui;

        private IEnumerator Open(int count)
        {
            yield return ARTestPlacement.ResetXR();
            EditorSceneManager.LoadSceneInPlayMode("Assets/MonopolyAR/Scenes/MonopolyAR_Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            game = Object.FindObjectOfType<MonopolyTurnManager>();
            ui = Object.FindObjectOfType<MonopolyPrototypeUI>();
            Assert.NotNull(game);
            Assert.NotNull(ui);
            game.activePlayerCount = count;
            yield return ARTestPlacement.PlaceDefault();
            Assert.AreEqual(TurnPhase.AwaitRoll, game.Phase);
        }

        [UnityTest]
        public IEnumerator ThreePlayersCycleAndKeepIndependentState()
        {
            yield return Open(3);
            Assert.AreEqual(3, game.players.Length);
            Assert.IsFalse(game.ConfiguredPlayers[3].gameObject.activeSelf, "Magnate no participa con tres jugadores");
            CollectionAssert.AreEqual(new[] { "Empresario", "Empresaria", "Constructor" },
                new[] { game.players[0].displayName, game.players[1].displayName, game.players[2].displayName });
            foreach (var p in game.players)
            {
                Assert.AreEqual(0, p.CurrentTile);
                Assert.AreEqual(MonopolyPlayer.StartingMoney, p.Money);
                Assert.AreEqual(0, p.LapsCompleted);
            }
            var constructor = game.players[2];
            constructor.PlaceAt(game.board, 39);
            yield return constructor.MoveSteps(game.board, 1, .005f);
            Assert.AreEqual(1700, constructor.Money);
            Assert.AreEqual(1, constructor.LapsCompleted);
            Assert.AreEqual(1500, game.players[0].Money);
            Assert.AreEqual(1500, game.players[1].Money);

            SetPhase(TurnPhase.AwaitEnd);
            Assert.IsTrue(game.TryEndTurn());
            Assert.AreEqual(1, game.ActivePlayerIndex);
            SetPhase(TurnPhase.AwaitEnd);
            Assert.IsTrue(game.TryEndTurn());
            Assert.AreEqual(2, game.ActivePlayerIndex);
            SetPhase(TurnPhase.AwaitEnd);
            Assert.IsTrue(game.TryEndTurn());
            Assert.AreEqual(0, game.ActivePlayerIndex);
        }

        [UnityTest]
        public IEnumerator FourPlayersCycleAndSeparateVisualStartPositions()
        {
            yield return Open(4);
            Assert.AreEqual(4, game.players.Length);
            for (int i = 0; i < game.ConfiguredPlayers.Length; i++) Assert.IsTrue(game.ConfiguredPlayers[i].gameObject.activeSelf);
            CollectionAssert.AreEqual(new[] { "Empresario", "Empresaria", "Constructor", "Magnate" },
                new[] { game.players[0].displayName, game.players[1].displayName, game.players[2].displayName, game.players[3].displayName });
            for (int i = 0; i < game.players.Length; i++)
            {
                Assert.AreEqual(0, game.players[i].CurrentTile);
                Assert.AreEqual(MonopolyPlayer.StartingMoney, game.players[i].Money);
                for (int j = i + 1; j < game.players.Length; j++)
                    Assert.Greater(Vector3.Distance(game.players[i].transform.position, game.players[j].transform.position), .001f);
            }
            foreach (var player in game.players)
            {
                player.PlaceAt(game.board, 39);
                yield return player.MoveSteps(game.board, 1, .005f);
                Assert.AreEqual(1700, player.Money, player.displayName + " debe cobrar al cruzar SALIDA");
                Assert.AreEqual(1, player.LapsCompleted, player.displayName + " debe contar su vuelta");
                player.ResetForNewGame();
                player.PlaceAt(game.board, 0);
            }
            for (int expected = 1; expected <= 4; expected++)
            {
                SetPhase(TurnPhase.AwaitEnd);
                Assert.IsTrue(game.TryEndTurn());
                Assert.AreEqual(expected % 4, game.ActivePlayerIndex);
            }
        }

        [UnityTest]
        public IEnumerator MagnateCanUseDiceAndOnlyActivePlayerMoves()
        {
            yield return Open(4);
            for (int i = 0; i < 3; i++)
            {
                SetPhase(TurnPhase.AwaitEnd);
                Assert.IsTrue(game.TryEndTurn());
            }
            Assert.AreEqual("Magnate", game.ActivePlayer.displayName);
            ui.RollButton.onClick.Invoke();
            float deadline = Time.realtimeSinceStartup + 15f;
            while (!game.CanEndTurn && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(game.CanEndTurn);
            Assert.That(game.dice.DieOne, Is.InRange(1, 6));
            Assert.That(game.dice.DieTwo, Is.InRange(1, 6));
            Assert.AreEqual(game.dice.Sum, game.players[3].CurrentTile);
            Assert.AreEqual("Magnate", game.ActivePlayer.displayName);
            Assert.IsTrue(ui.StatusText.text.Contains("Magnate"));
            Assert.AreEqual(0, game.players[0].CurrentTile);
            Assert.AreEqual(0, game.players[1].CurrentTile);
            Assert.AreEqual(0, game.players[2].CurrentTile);
        }

        private void SetPhase(TurnPhase phase)
        {
            var field = typeof(MonopolyTurnManager).GetField("<Phase>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(game, phase);
        }
    }
}
#endif
