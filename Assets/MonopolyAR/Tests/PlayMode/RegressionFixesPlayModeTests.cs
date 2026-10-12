#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace MonopolyAR.Tests
{
    public class RegressionFixesPlayModeTests
    {
        private MonopolyBoard board;
        private MonopolyTurnManager game;
        private PropertyCard3DController card;

        [UnitySetUp]
        public IEnumerator LoadBoardScene()
        {
            yield return ARTestPlacement.ResetXR();
            EditorSceneManager.LoadSceneInPlayMode("Assets/MonopolyAR/Scenes/MonopolyAR_Main.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            board = Object.FindObjectOfType<MonopolyBoard>(true);
            game = Object.FindObjectOfType<MonopolyTurnManager>(true);
            card = Object.FindObjectOfType<PropertyCard3DController>(true);
            Assert.NotNull(board);
            Assert.NotNull(game);
            Assert.NotNull(card);
            yield return ARTestPlacement.PlaceDefault();
        }

        [UnityTest]
        public IEnumerator TwoThreeAndFourPlayersCompleteTwoCircularRounds()
        {
            for (int count = 2; count <= 4; count++)
            {
                Assert.IsTrue(game.ConfigurePlayers(count));
                yield return null;
                Assert.AreEqual(count, game.players.Length);
                Assert.AreEqual(0, game.ActivePlayerIndex);
                for (int round = 0; round < 2; round++)
                {
                    for (int turn = 0; turn < count; turn++)
                    {
                        int activeBefore = game.ActivePlayerIndex;
                        int moneyBefore = game.ActivePlayer.Money;
                        Assert.IsTrue(game.TryRoll(), $"No se pudo lanzar en {count} jugadores, ronda {round}, turno {turn}");
                        float deadline = Time.realtimeSinceStartup + 15f;
                        while (!game.CanEndTurn && Time.realtimeSinceStartup < deadline) yield return null;
                        Assert.IsTrue(game.CanEndTurn, "El movimiento no terminó dentro del límite");
                        // A property card intentionally blocks the EndTurn action;
                        // this is the same close action a player performs on device.
                        if (card.IsOpen) card.Hide();
                        Assert.IsTrue(game.TryEndTurn());
                        Assert.AreEqual((activeBefore + 1) % count, game.ActivePlayerIndex);
                        Assert.That(game.players[activeBefore].Money, Is.GreaterThanOrEqualTo(0));
                        Assert.That(game.players[activeBefore].CurrentTile, Is.InRange(0, 39));
                    }
                }
                Assert.AreEqual(0, game.ActivePlayerIndex, $"El ciclo de {count} jugadores no volvió al Empresario");
                for (int i = 0; i < count; i++)
                {
                    Assert.IsTrue(game.players[i].gameObject.activeInHierarchy);
                    Assert.That(game.players[i].Money, Is.GreaterThanOrEqualTo(0));
                }
            }
        }

        [UnityTest]
        public IEnumerator RollSettlesBothDiceAboveBoardWithoutChangingLogicalResults()
        {
            Vector3 up = board.transform.up.normalized;
            float surface = Vector3.Dot(board.GetAnchor(0).position, up);
            float worldClearance = game.dice.SurfaceClearance * board.transform.TransformVector(Vector3.up).magnitude;
            for (int i = 0; i < 3; i++)
            {
                yield return game.dice.Roll();
                Assert.That(game.dice.DieOne, Is.InRange(1, 6));
                Assert.That(game.dice.DieTwo, Is.InRange(1, 6));
                Assert.AreEqual(game.dice.DieOne + game.dice.DieTwo, game.dice.Sum);
                AssertDieAboveSurface(game.dice.dieOneVisual, up, surface + worldClearance);
                AssertDieAboveSurface(game.dice.dieTwoVisual, up, surface + worldClearance);
            }
        }

        private static void AssertDieAboveSurface(Transform die, Vector3 up, float requiredMinimum)
        {
            var renderers = die.GetComponentsInChildren<Renderer>(true);
            float minimum = float.PositiveInfinity;
            foreach (var renderer in renderers)
            {
                Bounds bounds = renderer.bounds;
                float projectedExtent = Vector3.Dot(new Vector3(Mathf.Abs(up.x), Mathf.Abs(up.y), Mathf.Abs(up.z)), bounds.extents);
                minimum = Mathf.Min(minimum, Vector3.Dot(bounds.center, up) - projectedExtent);
            }
            Assert.GreaterOrEqual(minimum, requiredMinimum - 0.002f);
        }
    }
}
#endif
