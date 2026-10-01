#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace MonopolyAR.Tests
{
    public class PrototypePlayModeTests
    {
        private MonopolyTurnManager game;
        private MonopolyPrototypeUI ui;

        [UnitySetUp]
        public IEnumerator LoadPrototype()
        {
            yield return ARTestPlacement.ResetXR();
            EditorSceneManager.LoadSceneInPlayMode("Assets/MonopolyAR/Scenes/MonopolyAR_Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            game = Object.FindObjectOfType<MonopolyTurnManager>();
            ui = Object.FindObjectOfType<MonopolyPrototypeUI>();
            Assert.NotNull(game);
            yield return ARTestPlacement.PlaceDefault();
            Assert.AreEqual(TurnPhase.AwaitRoll, game.Phase);
        }

        [UnityTest]
        public IEnumerator PlayersStartAtOriginalStartWithSeparationWithoutAnimations()
        {
            Assert.AreEqual(2, game.players.Length);
            Assert.AreEqual("Empresario", game.players[0].displayName);
            Assert.AreEqual("Empresaria", game.players[1].displayName);
            Assert.AreEqual(40, game.board.Anchors.Length);
            for (int i = 0; i < 40; i++) Assert.AreEqual($"Tile_{i:00}_Anchor", game.board.GetAnchor(i).name);
            foreach (var p in game.players)
            {
                Assert.AreEqual(0, p.CurrentTile);
                Assert.AreEqual(MonopolyPlayer.StartingMoney, p.Money);
                Assert.Less(Vector3.Distance(p.transform.position, game.board.PositionFor(0, p.separation)), .001f);
                AssertStatic(p);
            }
            Assert.Greater(Vector3.Distance(game.players[0].transform.position, game.players[1].transform.position), .4f * game.board.transform.lossyScale.x);
            Assert.IsTrue(ui.RollButton.interactable);
            Assert.IsFalse(ui.EndTurnButton.interactable);
            yield return null;
        }

        [Test]
        public void ThousandDicePairsHaveValidFacesAndSums()
        {
            var old = Random.state;
            try
            {
                Random.InitState(91824);
                var seen = new bool[7];
                for (int i = 0; i < 1000; i++)
                {
                    var pair = MonopolyDice.GenerateRoll();
                    Assert.That(pair.x, Is.InRange(1, 6));
                    Assert.That(pair.y, Is.InRange(1, 6));
                    Assert.That(pair.x + pair.y, Is.InRange(2, 12));
                    seen[pair.x] = seen[pair.y] = true;
                }
                for (int face = 1; face <= 6; face++) Assert.IsTrue(seen[face]);
            }
            finally { Random.state = old; }
        }

        [UnityTest]
        public IEnumerator UiRollMovesEachAnchorThenEndTurnEnablesOtherPlayer()
        {
            Assert.IsFalse(game.TryEndTurn());
            ui.RollButton.onClick.Invoke();
            Assert.AreEqual(TurnPhase.Rolling, game.Phase);
            Assert.IsFalse(game.TryRoll());
            Assert.IsFalse(game.TryEndTurn());
            yield return null;
            Assert.IsFalse(ui.RollButton.interactable);
            Assert.IsFalse(ui.EndTurnButton.interactable);
            float timeout = Time.realtimeSinceStartup + 12;
            bool sawMovement = false;
            while (!game.CanEndTurn && Time.realtimeSinceStartup < timeout)
            {
                if (game.Phase == TurnPhase.Moving) { sawMovement |= game.ActivePlayer.IsMoving; AssertStatic(game.ActivePlayer); }
                yield return null;
            }
            Assert.IsTrue(game.CanEndTurn, "Timed out moving");
            Assert.IsTrue(sawMovement);
            int sum = game.dice.Sum;
            Assert.That(sum, Is.InRange(2, 12));
            Assert.AreEqual(game.dice.DieOne + game.dice.DieTwo, sum);
            Assert.AreEqual(sum, game.players[0].CurrentTile);
            CollectionAssert.AreEqual(Enumerable.Range(1, sum).ToArray(), game.players[0].LastVisited.ToArray());
            Assert.AreEqual(0, game.players[1].CurrentTile);
            Assert.Less(Vector3.Distance(game.players[0].transform.position, game.board.PositionFor(sum, game.players[0].separation)), .001f);
            yield return null;
            AssertStatic(game.players[0]);
            Assert.IsTrue(ui.EndTurnButton.interactable);
            Assert.IsFalse(ui.RollButton.interactable);
            Assert.IsFalse(game.TryRoll());
            ui.EndTurnButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(1, game.ActivePlayerIndex);
            Assert.IsTrue(ui.RollButton.interactable);
            Assert.IsFalse(ui.EndTurnButton.interactable);
            Assert.IsTrue(ui.StatusText.text.Contains("Empresaria"));
            Assert.IsTrue(game.TryRoll());
            timeout = Time.realtimeSinceStartup + 12;
            while (!game.CanEndTurn && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.IsTrue(game.CanEndTurn);
            Assert.AreEqual(game.dice.Sum, game.players[1].CurrentTile);
            Assert.AreEqual(sum, game.players[0].CurrentTile);
            Assert.IsTrue(game.TryEndTurn());
            Assert.AreEqual(0, game.ActivePlayerIndex);
        }

        [UnityTest]
        public IEnumerator MovementFrom39Visits0Then1UsingOriginalAnchors()
        {
            var player = game.players[0];
            player.PlaceAt(game.board, 39);
            yield return player.MoveSteps(game.board, 2, .03f);
            CollectionAssert.AreEqual(new[] { 0, 1 }, player.LastVisited.ToArray());
            Assert.AreEqual(1, player.CurrentTile);
            Assert.Less(Vector3.Distance(player.transform.position, game.board.PositionFor(1, player.separation)), .001f);
            Assert.IsFalse(player.IsMoving);
            yield return null;
            AssertStatic(player);
        }

        [UnityTest]
        public IEnumerator FullCircuitVisitsAll40AndReturnsToStart()
        {
            var player = game.players[1];
            var parts = player.GetComponentsInChildren<Transform>(true).Where(t => t != player.transform).ToArray();
            var positions = parts.Select(t => t.localPosition).ToArray();
            var rotations = parts.Select(t => t.localRotation).ToArray();
            yield return player.MoveSteps(game.board, 40, .02f);
            CollectionAssert.AreEqual(Enumerable.Range(1, 39).Concat(new[] { 0 }).ToArray(), player.LastVisited.ToArray());
            Assert.AreEqual(0, player.CurrentTile);
            Assert.Less(Vector3.Distance(player.transform.position, game.board.PositionFor(0, player.separation)), .001f);
            for (int i = 0; i < parts.Length; i++)
            {
                Assert.AreEqual(positions[i], parts[i].localPosition, parts[i].name);
                Assert.AreEqual(rotations[i], parts[i].localRotation, parts[i].name);
            }
            AssertStatic(player);
        }

        private static void AssertStatic(MonopolyPlayer player)
        {
            Assert.IsEmpty(player.GetComponentsInChildren<Animation>(true));
            Assert.IsEmpty(player.GetComponentsInChildren<Animator>(true));
            Assert.IsNotEmpty(player.GetComponentsInChildren<MeshRenderer>(true));
        }

        [Test]
        public void AllFourOriginalPrefabsAreStaticAndKeepMaterials()
        {
            foreach (var name in new[] { "Empresario", "Empresaria", "Constructor", "Magnate" })
            {
                var model = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MonopolyAR/Prefabs/Characters/" + name + ".prefab");
                Assert.NotNull(model, name);
                Assert.IsEmpty(model.GetComponentsInChildren<Animation>(true));
                Assert.IsEmpty(model.GetComponentsInChildren<Animator>(true));
                foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
                    Assert.IsTrue(renderer.sharedMaterials.All(m => m && m.shader && m.shader.name == "Universal Render Pipeline/Lit"), renderer.name);
            }
            Assert.IsEmpty(UnityEditor.AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/MonopolyAR/Animations", "Assets/MonopolyAR/Models" }));
        }
    }
}
#endif
