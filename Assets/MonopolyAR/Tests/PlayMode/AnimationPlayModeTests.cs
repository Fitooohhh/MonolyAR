using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

namespace MonopolyAR.Tests
{
    public class AnimationPlayModeTests
    {
        [UnityTest]
        public IEnumerator WalkingFlagStartsDuringMovementAndReturnsToIdle()
        {
            yield return ARTestPlacement.ResetXR();
            EditorSceneManager.LoadSceneInPlayMode("Assets/MonopolyAR/Scenes/MonopolyAR_Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var game = Object.FindObjectOfType<MonopolyTurnManager>(true);
            Assert.NotNull(game);
            yield return ARTestPlacement.PlaceDefault();
            var player = game.players[0];
            var motion = player.MoveSteps(game.board, 2, .08f);
            player.StartCoroutine(motion);
            yield return null;
            Assert.IsTrue(player.IsWalkingAnimationActive);
            while (player.IsMoving) yield return null;
            Assert.IsFalse(player.IsWalkingAnimationActive);
        }
    }
}
