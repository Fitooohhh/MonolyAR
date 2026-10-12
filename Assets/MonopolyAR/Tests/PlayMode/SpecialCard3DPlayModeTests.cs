using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

namespace MonopolyAR.Tests
{
    public class SpecialCard3DPlayModeTests
    {
        private MonopolyBoard board;
        private MonopolyTurnManager game;
        private MonopolyPrototypeUI ui;
        private SpecialCard3DController card;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return ARTestPlacement.ResetXR();
            EditorSceneManager.LoadSceneInPlayMode("Assets/MonopolyAR/Scenes/MonopolyAR_Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            board = Object.FindObjectOfType<MonopolyBoard>(true);
            game = Object.FindObjectOfType<MonopolyTurnManager>(true);
            ui = Object.FindObjectOfType<MonopolyPrototypeUI>(true);
            card = Object.FindObjectOfType<SpecialCard3DController>(true);
            Assert.NotNull(board); Assert.NotNull(game); Assert.NotNull(ui); Assert.NotNull(card);
            yield return ARTestPlacement.PlaceDefault();
        }

        [UnityTest]
        public IEnumerator EverySpecialTypeOpensOneReusableCard()
        {
            var player = game.players[0];
            SpaceType[] types = { SpaceType.Chance, SpaceType.Community, SpaceType.Tax, SpaceType.Jail, SpaceType.FreeParking, SpaceType.GoToJail };
            int[] indices = { 7, 2, 4, 10, 20, 30 };
            for (int i = 0; i < types.Length; i++)
            {
                card.Show(player, board.GetSpaceData(indices[i]));
                Assert.IsTrue(card.IsOpen);
                Assert.AreEqual(types[i], card.CurrentType);
                Assert.IsFalse(string.IsNullOrEmpty(card.Summary));
                card.Acknowledge();
                Assert.IsFalse(card.IsOpen);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SpecialCardReusesPropertyCardVisualAndClosePath()
        {
            var property = Object.FindObjectOfType<PropertyCard3DController>(true);
            Assert.NotNull(property);
            Assert.AreSame(property, card.VisualCard);

            card.Show(game.players[0], board.GetSpaceData(7));
            yield return null;
            Assert.IsTrue(card.IsOpen);
            Assert.IsTrue(property.IsOpen);
            Assert.IsTrue(property.transform.Find("FrontContent").gameObject.activeSelf);

            card.Acknowledge();
            yield return null;
            Assert.IsFalse(card.IsOpen);
            Assert.IsFalse(property.IsOpen);
        }

        [UnityTest]
        public IEnumerator TaxAndGoToJailApplyExactlyOnce()
        {
            var player = game.players[0];
            int before = player.Money;
            card.Show(player, board.GetSpaceData(4));
            Assert.AreEqual(before - 200, player.Money);
            card.Show(player, board.GetSpaceData(4));
            Assert.AreEqual(before - 400, player.Money);
            card.Acknowledge();
            player.PlaceAt(board, 30);
            card.Show(player, board.GetSpaceData(30));
            Assert.IsTrue(player.IsInJail);
            Assert.AreEqual(10, player.CurrentTile);
            card.Acknowledge();
            yield return null;
        }

        [UnityTest]
        public IEnumerator JailOptionsUseScreenSpaceButtonsAndUnlockOnClose()
        {
            var player = game.players[0];
            player.SendToJail(board);
            card.Show(player, board.GetSpaceData(10));
            yield return null;
            Assert.IsTrue(card.JailOptionsVisible);
            Assert.IsTrue(ui.JailPayButton.gameObject.activeSelf);
            Assert.IsTrue(ui.JailRollButton.gameObject.activeSelf);
            Assert.IsTrue(ui.JailCardButton.gameObject.activeSelf);
            ui.JailPayButton.onClick.Invoke();
            Assert.IsFalse(player.IsInJail);
            Assert.IsFalse(card.IsOpen);
        }
    }
}
