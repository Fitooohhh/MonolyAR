#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

namespace MonopolyAR.Tests
{
    public class SpaceDataPlayModeTests
    {
        private MonopolyBoard board;

        [UnitySetUp]
        public IEnumerator LoadBoardScene()
        {
            yield return ARTestPlacement.ResetXR();
            EditorSceneManager.LoadSceneInPlayMode("Assets/MonopolyAR/Scenes/MonopolyAR_Main.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            board = Object.FindObjectOfType<MonopolyBoard>(true);
            Assert.NotNull(board);
            board.EnsureSpaceData();
        }

        [Test]
        public void DefaultCatalogHasFortyUniqueIndicesAndRequiredGroups()
        {
            var spaces = MonopolySpaceDataCatalog.CreateDefault();
            Assert.AreEqual(MonopolyBoard.TileCount, spaces.Length);
            CollectionAssert.AreEqual(Enumerable.Range(0, 40), spaces.Select(s => s.index));
            Assert.AreEqual(40, spaces.Select(s => s.index).Distinct().Count());

            AssertGroup(spaces, PropertyColorGroup.Brown, 1, 3);
            AssertGroup(spaces, PropertyColorGroup.LightBlue, 6, 8, 9);
            AssertGroup(spaces, PropertyColorGroup.Pink, 11, 13, 14);
            AssertGroup(spaces, PropertyColorGroup.Orange, 16, 18, 19);
            AssertGroup(spaces, PropertyColorGroup.Red, 21, 23, 24);
            AssertGroup(spaces, PropertyColorGroup.Yellow, 26, 27, 29);
            AssertGroup(spaces, PropertyColorGroup.Green, 31, 32, 34);
            AssertGroup(spaces, PropertyColorGroup.DarkBlue, 37, 39);
        }

        [UnityTest]
        public IEnumerator SceneHasFortyAnchorsAndFortySpaceDataWithMatchingIndices()
        {
            Assert.AreEqual(40, board.Anchors.Length);
            for (int i = 0; i < MonopolyBoard.TileCount; i++)
                Assert.AreEqual($"Tile_{i:00}_Anchor", board.GetAnchor(i).name);

            var spaces = board.Spaces;
            Assert.AreEqual(40, spaces.Length);
            CollectionAssert.AreEqual(Enumerable.Range(0, 40), spaces.Select(s => s.index));
            yield return null;
        }

        [UnityTest]
        public IEnumerator GetSpaceDataReturnsExpectedSpecialSpaces()
        {
            AssertSpace(0, "SALIDA", SpaceType.Start, PropertyColorGroup.None);
            AssertSpace(1, "Propiedad 01", SpaceType.Property, PropertyColorGroup.Brown);
            AssertSpace(5, "Estación 1", SpaceType.Station, PropertyColorGroup.None);
            AssertSpace(10, "Cárcel / Visita", SpaceType.Jail, PropertyColorGroup.None);
            AssertSpace(12, "Electricidad", SpaceType.Utility, PropertyColorGroup.None);
            AssertSpace(20, "Parking", SpaceType.FreeParking, PropertyColorGroup.None);
            AssertSpace(21, "Propiedad 21", SpaceType.Property, PropertyColorGroup.Red);
            AssertSpace(28, "Agua", SpaceType.Utility, PropertyColorGroup.None);
            AssertSpace(30, "Ir a la Cárcel", SpaceType.GoToJail, PropertyColorGroup.None);
            AssertSpace(37, "Propiedad 37", SpaceType.Property, PropertyColorGroup.DarkBlue);
            AssertSpace(39, "Propiedad 39", SpaceType.Property, PropertyColorGroup.DarkBlue);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ArrivalEventReportsFinalSpaceAfterMovement()
        {
            var player = Object.FindObjectOfType<MonopolyTurnManager>().players[0];
            MonopolySpaceData arrived = default;
            MonopolyPlayer arrivedPlayer = null;
            player.ArrivedAtSpace += (p, data) => { arrivedPlayer = p; arrived = data; };
            player.PlaceAt(board, 39);
            yield return player.MoveSteps(board, 1, .01f);
            Assert.AreSame(player, arrivedPlayer);
            Assert.AreEqual(0, arrived.index);
            Assert.AreEqual(SpaceType.Start, arrived.type);
            Assert.AreEqual("SALIDA", arrived.displayName);
        }

        private void AssertSpace(int index, string name, SpaceType type, PropertyColorGroup color)
        {
            var data = board.GetSpaceData(index);
            Assert.AreEqual(index, data.index);
            Assert.AreEqual(name, data.displayName);
            Assert.AreEqual(type, data.type);
            Assert.AreEqual(color, data.colorGroup);
        }

        private static void AssertGroup(IEnumerable<MonopolySpaceData> spaces, PropertyColorGroup group, params int[] expected)
        {
            CollectionAssert.AreEquivalent(expected, spaces.Where(s => s.colorGroup == group).Select(s => s.index));
        }
    }
}
#endif
