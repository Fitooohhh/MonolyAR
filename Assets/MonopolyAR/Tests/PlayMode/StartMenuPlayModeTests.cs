#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

namespace MonopolyAR.Tests
{
    public class StartMenuPlayModeTests
    {
        private MonopolyTurnManager game;
        private MonopolyPrototypeUI ui;

        private IEnumerator OpenMenu()
        {
            yield return ARTestPlacement.ResetXR();
            EditorSceneManager.LoadSceneInPlayMode("Assets/MonopolyAR/Scenes/MonopolyAR_Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            game = Object.FindObjectOfType<MonopolyTurnManager>();
            ui = Object.FindObjectOfType<MonopolyPrototypeUI>();
            Assert.NotNull(game);
            Assert.NotNull(ui);
            Assert.IsTrue(ui.IsStartMenuVisible);
            Assert.IsFalse(game.enabled);
            Assert.AreEqual(TurnPhase.NotReady, game.Phase);
        }

        [UnityTest]
        public IEnumerator MenuStartsTwoPlayers()
        {
            yield return OpenMenu();
            yield return StartSelectedCount(2);
            Assert.AreEqual(2, game.players.Length);
            Assert.IsTrue(game.players[0].gameObject.activeInHierarchy);
            Assert.IsTrue(game.players[1].gameObject.activeInHierarchy);
            Assert.IsFalse(game.ConfiguredPlayers[2].gameObject.activeInHierarchy);
            Assert.IsFalse(game.ConfiguredPlayers[3].gameObject.activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator MenuStartsThreePlayers()
        {
            yield return OpenMenu();
            yield return StartSelectedCount(3);
            Assert.AreEqual(3, game.players.Length);
            Assert.AreEqual("Constructor", game.players[2].displayName);
            Assert.IsTrue(game.players[2].gameObject.activeInHierarchy);
            Assert.IsNotEmpty(game.players[2].GetComponentsInChildren<MeshRenderer>(true));
            Assert.IsTrue(System.Array.TrueForAll(game.players[2].GetComponentsInChildren<MeshRenderer>(true), r => r.enabled));
            Assert.IsFalse(game.ConfiguredPlayers[3].gameObject.activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator MenuStartsFourPlayers()
        {
            yield return OpenMenu();
            yield return StartSelectedCount(4);
            Assert.AreEqual(4, game.players.Length);
            Assert.AreEqual("Magnate", game.players[3].displayName);
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(game.players[i].gameObject.activeInHierarchy);
                Assert.AreEqual(1500, game.players[i].Money);
                Assert.AreEqual(0, game.players[i].CurrentTile);
                Assert.AreEqual(0, game.players[i].LapsCompleted);
                Assert.IsNotEmpty(game.players[i].GetComponentsInChildren<MeshRenderer>(true));
                Assert.IsTrue(System.Array.TrueForAll(game.players[i].GetComponentsInChildren<MeshRenderer>(true), r => r.enabled));
            }
        }

        private IEnumerator StartSelectedCount(int count)
        {
            ui.PlayerCountButtons[count - 2].onClick.Invoke();
            yield return null;
            Assert.AreEqual(count, ui.SelectedPlayerCount);
            Assert.IsTrue(ui.PlayButton.interactable);
            ui.PlayButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(ui.IsStartMenuVisible);
            Assert.AreEqual(count, game.activePlayerCount);
            Assert.IsFalse(game.enabled, "El juego espera la colocación AR");
            yield return ARTestPlacement.PlaceDefault();
            Assert.AreEqual(TurnPhase.AwaitRoll, game.Phase);
            Assert.IsTrue(ui.RollButton.interactable);
        }
    }
}
#endif
