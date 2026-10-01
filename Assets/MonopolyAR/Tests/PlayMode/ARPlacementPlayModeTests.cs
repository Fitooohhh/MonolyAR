#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.XR.ARFoundation;

namespace MonopolyAR.Tests
{
    internal static class ARTestPlacement
    {
        internal static IEnumerator ResetXR()
        {
            // XR Simulation 5.2 caches the camera across scene reloads. Recreate its
            // loader between tests so each scene receives a fresh subsystem.
            foreach (var session in Object.FindObjectsOfType<ARSession>()) session.enabled = false;
            foreach (var camera in Object.FindObjectsOfType<ARCameraManager>()) camera.enabled = false;
            foreach (var planes in Object.FindObjectsOfType<ARPlaneManager>()) planes.enabled = false;
            foreach (var rays in Object.FindObjectsOfType<ARRaycastManager>()) rays.enabled = false;
            foreach (var anchors in Object.FindObjectsOfType<ARAnchorManager>()) anchors.enabled = false;
            var settings = UnityEngine.XR.Management.XRGeneralSettings.Instance;
            if (settings && settings.Manager)
            {
                settings.Manager.StopSubsystems();
                settings.Manager.DeinitializeLoader();
                yield return null;
                settings.Manager.InitializeLoaderSync();
            }
        }

        // Inject only the measured pose in tests. Real device input still requires a raycast and AR anchor.
        internal static bool Place(ARBoardPlacement placement, Pose pose)
        {
            return (bool)typeof(ARBoardPlacement).GetMethod("PlaceBoard", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(placement, new object[] { pose, null });
        }
        internal static IEnumerator PlaceDefault()
        {
            var placement = Object.FindObjectOfType<ARBoardPlacement>();
            Assert.NotNull(placement);
            var ui = Object.FindObjectOfType<MonopolyPrototypeUI>();
            var game = Object.FindObjectOfType<MonopolyTurnManager>();
            if (ui && ui.IsStartMenuVisible) Assert.IsTrue(ui.StartGame(game.activePlayerCount));
            Assert.IsTrue(Place(placement, new Pose(new Vector3(.3f,.8f,-.6f), Quaternion.Euler(0,37,0))));
            yield return null;
            yield return null;
        }
    }

    public class ARPlacementPlayModeTests
    {
        private ARBoardPlacement placement;
        private MonopolyPrototypeUI ui;
        private MonopolyTurnManager game;
        [UnitySetUp]
        public IEnumerator Open()
        {
            yield return ARTestPlacement.ResetXR();
            EditorSceneManager.LoadSceneInPlayMode("Assets/MonopolyAR/Scenes/MonopolyAR_Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            placement = Object.FindObjectOfType<ARBoardPlacement>();
            ui = Object.FindObjectOfType<MonopolyPrototypeUI>();
            game = placement.game;
        }

        [UnityTest]
        public IEnumerator HiddenBoardRejectsRollAndKeepsFullScreenARCamera()
        {
            Assert.IsFalse(placement.BoardPlaced);
            Assert.IsFalse(placement.boardRoot.gameObject.activeSelf);
            Assert.IsFalse(game.enabled);
            Assert.AreEqual(TurnPhase.NotReady,game.Phase);
            Assert.IsFalse(ui.RollButton.interactable);
            ui.RollButton.onClick.Invoke();
            Assert.IsFalse(game.TryRoll());
            Assert.AreEqual(TurnPhase.NotReady,game.Phase);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay,ui.GetComponentInChildren<Canvas>().renderMode);
            Assert.NotNull(placement.arCamera.GetComponent<ARCameraBackground>());
            Assert.NotNull(placement.arCamera.GetComponent<ARCameraManager>());
            var before = placement.arCamera.transform.position;
            yield return null;
            Assert.IsFalse(placement.arCamera.orthographic);
            Assert.AreEqual(new Rect(0,0,1,1),placement.arCamera.rect);
            Assert.AreEqual(before,placement.arCamera.transform.position, "UI must not fit or move AR camera");
            Assert.AreEqual(40,game.board.Anchors.Length);
            game.board.Validate();
        }

        [UnityTest]
        public IEnumerator PlacementIsUniqueAndScalesExistingBoardPlayersDiceAndAnchors()
        {
            var anchors=game.board.Anchors;
            var local=anchors.Select(a=>game.board.transform.InverseTransformPoint(a.position)).ToArray();
            var boardId=game.board.GetInstanceID();
            var diceId=game.dice.GetInstanceID();
            var pose = new Pose(new Vector3(1,.75f,-1),Quaternion.Euler(0,63,0));
            if (ui.IsStartMenuVisible) Assert.IsTrue(ui.StartGame(game.activePlayerCount));
            Assert.IsTrue(ARTestPlacement.Place(placement,pose));
            yield return null;
            yield return null;
            Assert.AreEqual(boardId,game.board.GetInstanceID());
            Assert.AreEqual(diceId,game.dice.GetInstanceID());
            Assert.AreEqual(1,Object.FindObjectsOfType<MonopolyBoard>().Length);
            Assert.IsTrue(placement.BoardPlaced);
            Assert.IsTrue(game.enabled);
            Assert.IsTrue(ui.RollButton.interactable);
            Assert.IsFalse(placement.indicator.gameObject.activeSelf);
            Assert.Less(Vector3.Distance(Vector3.up,game.board.transform.up),.00001f);
            Assert.AreEqual(.6f,14*game.board.transform.lossyScale.x,.00001f);
            Assert.IsTrue(game.dice.transform.IsChildOf(game.board.transform));
            foreach(var p in game.players)
            {
                Assert.IsTrue(p.transform.IsChildOf(game.board.transform));
                Assert.AreEqual(0,p.CurrentTile);
                Assert.Less(Vector3.Distance(p.transform.position,game.board.PositionFor(0,p.separation)),.00001f);
            }
            for(int i=0;i<40;i++)
            {
                Assert.AreSame(anchors[i],game.board.GetAnchor(i));
                Assert.Less(Vector3.Distance(anchors[i].position,game.board.transform.TransformPoint(local[i])),.00001f);
            }
            Vector3 towardCamera = Vector3.ProjectOnPlane(placement.arCamera.transform.position - pose.position, pose.rotation * Vector3.up).normalized;
            Assert.Greater(towardCamera.sqrMagnitude, .99f);
            Assert.Less(Vector3.Angle(game.board.transform.forward, towardCamera), .001f);
            var oldPosition=game.board.transform.position;
            var oldRotation=game.board.transform.rotation;
            Assert.IsFalse(ARTestPlacement.Place(placement,new Pose(Vector3.one*99,Quaternion.identity)));
            Assert.AreEqual(oldPosition,game.board.transform.position);
            Assert.AreEqual(oldRotation,game.board.transform.rotation);
            game.players[0].PlaceAt(game.board,39);
            yield return game.players[0].MoveSteps(game.board,2,.03f);
            CollectionAssert.AreEqual(new[]{0,1},game.players[0].LastVisited);
            Assert.Less(Vector3.Distance(game.players[0].transform.position,game.board.PositionFor(1,game.players[0].separation)),.00001f);
        }
    }
}
#endif
