#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor.SceneManagement;

namespace MonopolyAR.Tests
{
    public class PropertyCard3DPlayModeTests
    {
        private MonopolyBoard board;
        private MonopolyTurnManager game;
        private PropertyCard3DController card;
        private MonopolyPrototypeUI ui;

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
            ui = Object.FindObjectOfType<MonopolyPrototypeUI>(true);
            Assert.NotNull(board);
            Assert.NotNull(game);
            Assert.NotNull(card);
            Assert.NotNull(ui);
            yield return ARTestPlacement.PlaceDefault();
        }

        [UnityTest]
        public IEnumerator PropertyOpensCardWithMatchingDataAndHeaderColor()
        {
            var data = board.GetSpaceData(21);
            Assert.AreEqual(SpaceType.Property, data.type);
            card.Show(game.players[0], data);
            yield return null;

            Assert.IsTrue(card.IsOpen);
            Assert.AreEqual(data.index, card.CurrentData.index);
            Assert.AreEqual(data.displayName.ToUpperInvariant(), card.transform.Find("WorldSpaceCanvas/PropertyNameText").GetComponent<Text>().text);
            Assert.AreEqual("RED", card.transform.Find("WorldSpaceCanvas/GroupText").GetComponent<Text>().text);
            var expectedColor = PropertyCard3DController.HeaderColor(PropertyColorGroup.Red);
            var actualColor = card.ColorHeaderRenderer.material.color;
            Assert.That(actualColor.r, Is.EqualTo(expectedColor.r).Within(0.01f));
            Assert.That(actualColor.g, Is.EqualTo(expectedColor.g).Within(0.01f));
            Assert.That(actualColor.b, Is.EqualTo(expectedColor.b).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator ArrivalAtPropertyRaisesEventAndOpensReusableCard()
        {
            card.Hide();
            game.players[0].PlaceAt(board, 0);
            yield return game.players[0].MoveSteps(board, 1, .005f);
            yield return null;
            Assert.IsTrue(card.IsOpen);
            Assert.AreEqual(1, card.CurrentData.index);
            Assert.AreEqual(SpaceType.Property, card.CurrentData.type);
        }

        [UnityTest]
        public IEnumerator PurchaseRegistersOwnerAndPreventsDuplicatePurchase()
        {
            var buyer = game.players[0];
            var other = game.players[1];
            var data = board.GetSpaceData(1);
            int before = buyer.Money;
            card.Show(buyer, data);
            yield return null;
            Assert.IsTrue(card.PurchaseDecisionPending);
            Assert.IsTrue(card.PurchaseCurrent());
            Assert.AreEqual(buyer, board.GetOwner(data.index));
            Assert.AreEqual(before - data.purchasePrice, buyer.Money);
            card.Show(other, data);
            yield return null;
            Assert.IsFalse(card.PurchaseDecisionPending);
            Assert.IsFalse(card.PurchaseCurrent());
            Assert.AreEqual(before - data.purchasePrice, buyer.Money);
        }

        [UnityTest]
        public IEnumerator DecliningPurchaseKeepsMoneyAndReleasesCard()
        {
            var buyer = game.players[0];
            var data = board.GetSpaceData(3);
            int before = buyer.Money;
            card.Show(buyer, data);
            yield return null;
            card.DeclinePurchase();
            Assert.AreEqual(before, buyer.Money);
            Assert.IsNull(board.GetOwner(data.index));
            Assert.IsFalse(card.IsOpen);
        }

        [UnityTest]
        public IEnumerator ClassicPurchasePricesMatchTheFortyBoardSpaces()
        {
            int[] expected = { 0, 60, 0, 60, 0, 200, 100, 0, 100, 120,
                0, 140, 150, 140, 160, 200, 180, 0, 180, 200,
                0, 220, 0, 220, 240, 200, 260, 260, 150, 280,
                0, 300, 300, 0, 320, 200, 0, 350, 0, 400 };
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], board.GetSpaceData(i).purchasePrice, "Precio incorrecto en " + i);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PurchaseDeductsExactPriceAndInsufficientFundsIsRejected()
        {
            var buyer = game.players[0];
            var data = board.GetSpaceData(39);
            int before = buyer.Money;
            card.Show(buyer, data);
            yield return null;
            Assert.IsTrue(card.PurchaseCurrent());
            Assert.AreEqual(before - 400, buyer.Money);

            var poor = game.players[1];
            poor.TrySpend(poor.Money);
            card.Show(poor, board.GetSpaceData(37));
            yield return null;
            Assert.IsFalse(card.CanPurchaseCurrent);
            Assert.IsFalse(card.PurchaseCurrent());
            Assert.AreEqual(0, poor.Money);
            Assert.IsNull(board.GetOwner(37));
        }

        [UnityTest]
        public IEnumerator PropertyRentTransfersBaseRentOnce()
        {
            var owner = game.players[0];
            var visitor = game.players[1];
            Assert.IsTrue(board.TryPurchase(owner, 1, out _));
            int ownerBefore = owner.Money;
            int visitorBefore = visitor.Money;
            Assert.IsTrue(board.TryCollectRent(visitor, 1, 7, out var rentOwner, out int rent, out int paid, out int debt, out _));
            Assert.AreEqual(owner, rentOwner);
            Assert.AreEqual(2, rent);
            Assert.AreEqual(2, paid);
            Assert.AreEqual(0, debt);
            Assert.AreEqual(ownerBefore + 2, owner.Money);
            Assert.AreEqual(visitorBefore - 2, visitor.Money);
            Assert.IsFalse(board.TryCollectRent(visitor, 1, 7, out _, out _, out _, out _, out _));
            yield return null;
        }

        [UnityTest]
        public IEnumerator StationsAndUtilitiesUseOwnedCountMultipliers()
        {
            var owner = game.players[0];
            var visitor = game.players[1];
            Assert.IsTrue(board.TryPurchase(owner, 5, out _));
            Assert.IsTrue(board.TryPurchase(owner, 15, out _));
            Assert.IsTrue(board.TryCollectRent(visitor, 5, 8, out _, out int stationRent, out _, out _, out _));
            Assert.AreEqual(50, stationRent);
            board.ClearRentResolutions();
            Assert.IsTrue(board.TryPurchase(owner, 12, out _));
            Assert.IsTrue(board.TryCollectRent(visitor, 12, 8, out _, out int utilityRent, out _, out _, out _));
            Assert.AreEqual(32, utilityRent);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InsufficientFundsCreatesDebtWithoutNegativeBalance()
        {
            var owner = game.players[0];
            var visitor = game.players[1];
            Assert.IsTrue(board.TryPurchase(owner, 39, out _));
            visitor.TrySpend(visitor.Money);
            Assert.IsTrue(board.TryCollectRent(visitor, 39, 0, out _, out int rent, out int paid, out int debt, out _));
            Assert.AreEqual(50, rent);
            Assert.AreEqual(0, paid);
            Assert.AreEqual(50, debt);
            Assert.AreEqual(0, visitor.Money);
            Assert.AreEqual(50, board.GetOutstandingDebt(visitor));
            yield return null;
        }

        [UnityTest]
        public IEnumerator StationAndJailPurchaseRulesOpenOnlyPurchasableSpaces()
        {
            card.Show(game.players[0], board.GetSpaceData(1));
            yield return null;
            Assert.IsTrue(card.IsOpen);

            card.Show(game.players[0], board.GetSpaceData(5));
            Assert.IsTrue(card.IsOpen);
            card.Close();
            card.Show(game.players[0], board.GetSpaceData(10));
            Assert.IsFalse(card.IsOpen);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ScreenSpaceCloseUsesTheSingleReusableInstance()
        {
            Assert.AreEqual(1, Object.FindObjectsOfType<PropertyCard3DController>(true).Length);
            card.Show(game.players[0], board.GetSpaceData(1));
            yield return null;
            card.Close();
            Assert.IsFalse(card.IsOpen);
            Assert.IsFalse(card.transform.Find("Model").gameObject.activeSelf);
            Assert.IsFalse(card.transform.Find("WorldSpaceCanvas").gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator ThreeDimensionalCloseControlIsRemoved()
        {
            card.Show(game.players[0], board.GetSpaceData(1));
            yield return null;
            Assert.IsNull(card.transform.Find("FrontContent/Close"));
            Assert.IsFalse(card.transform.Find("WorldSpaceCanvas/CloseButton").gameObject.activeSelf);
            Assert.IsNull(card.transform.Find("FrontContent/Close")?.GetComponent<Collider>());
            card.Close();
            Assert.IsFalse(card.IsOpen);
        }

        [UnityTest]
        public IEnumerator OpenCardBlocksTurnActionsUntilClosed()
        {
            card.Show(game.players[0], board.GetSpaceData(1));
            yield return null;
            Assert.IsFalse(ui.RollButton.interactable);
            Assert.IsFalse(ui.EndTurnButton.interactable);
            card.Hide();
            yield return null;
            Assert.IsTrue(ui.RollButton.interactable);
        }

        [UnityTest]
        public IEnumerator VisibleCardPassesCameraRendererCanvasAndWorldSizeValidation()
        {
            card.Show(game.players[0], board.GetSpaceData(21));
            yield return new WaitForSecondsRealtime(0.65f);
            Assert.IsTrue(card.IsOpen, card.LastPresentationDiagnostics);
            Assert.IsTrue(card.LastPresentationValid, card.LastPresentationDiagnostics);
            Assert.IsTrue(card.ValidatePresentation(out string diagnostics), diagnostics);
            Assert.That(card.TargetWorldHeight, Is.InRange(0.20f, 0.24f));
            foreach (Renderer renderer in card.transform.Find("Model").GetComponentsInChildren<Renderer>(true))
            {
                Assert.IsTrue(renderer.enabled);
                Assert.IsFalse(renderer.forceRenderingOff);
                Assert.IsTrue(renderer.gameObject.activeInHierarchy);
            }
            var canvas = card.transform.Find("WorldSpaceCanvas").GetComponent<Canvas>();
            Assert.IsTrue(canvas.isActiveAndEnabled);
            Assert.AreSame(Camera.main, canvas.worldCamera);
            Assert.IsFalse(canvas.overrideSorting, "El Canvas debe respetar la profundidad 3D del modelo");
            Assert.Less(card.transform.Find("WorldSpaceCanvas").lossyScale.x, card.transform.Find("Model").lossyScale.x,
                "El Canvas no debe escalarse por encima del modelo");
        }

        [UnityTest]
        public IEnumerator ScreenSpaceFallbackClosesTheSameCardAndRestoresControls()
        {
            card.Show(game.players[0], board.GetSpaceData(1));
            yield return null;
            Assert.IsTrue(card.IsOpen, card.LastPresentationDiagnostics);
            Assert.NotNull(ui.FallbackCloseCardButton);
            Assert.IsTrue(ui.FallbackCloseCardButton.gameObject.activeInHierarchy);
            ui.FallbackCloseCardButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(card.IsOpen);
            Assert.IsFalse(ui.FallbackCloseCardButton.gameObject.activeSelf);
            Assert.IsTrue(ui.RollButton.interactable);
        }

        [UnityTest]
        public IEnumerator ScreenSpaceFallbackIsReachableThroughTheOverlayRaycaster()
        {
            card.Show(game.players[0], board.GetSpaceData(1));
            yield return null;
            var button = ui.FallbackCloseCardButton;
            var overlay = ui.GetComponentInChildren<Canvas>();
            var raycaster = overlay.GetComponent<GraphicRaycaster>();
            Assert.IsTrue(button.gameObject.activeInHierarchy);
            Assert.NotNull(raycaster);
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position)
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(eventData, hits);
            Assert.IsTrue(hits.Exists(hit => hit.gameObject == button.gameObject || hit.gameObject.transform.IsChildOf(button.transform)));
            ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.IsFalse(card.IsOpen);
        }

        [UnityTest]
        public IEnumerator PresentationFailureDoesNotLeaveTurnBlocked()
        {
            FieldInfo modelField = typeof(PropertyCard3DController).GetField("modelRoot", BindingFlags.Instance | BindingFlags.NonPublic);
            var originalModel = (Transform)modelField.GetValue(card);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("No se pudo mostrar la tarjeta.*libera el turno"));
            modelField.SetValue(card, null);
            card.Show(game.players[0], board.GetSpaceData(1));
            modelField.SetValue(card, originalModel);
            yield return null;
            Assert.IsFalse(card.IsOpen);
            Assert.IsFalse(card.LastPresentationValid);
            Assert.IsTrue(ui.RollButton.interactable);
        }

        [Test]
        public void ModelScaleAndCurrentCatalogRemainIntact()
        {
            Assert.That(card.ModelScale, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.AreEqual(40, board.Spaces.Length);
            Assert.AreEqual(40, board.Anchors.Length);
        }

        [UnityTest]
        public IEnumerator CardIsAboveBoardAndFacesCameraAfterOpening()
        {
            card.Show(game.players[0], board.GetSpaceData(21));
            yield return new WaitForSeconds(0.65f);
            var renderers = card.transform.Find("Model").GetComponentsInChildren<Renderer>(true);
            Assert.IsTrue(renderers.Length > 0);
            Vector3 up = board.transform.up.normalized;
            float minimum = float.PositiveInfinity;
            foreach (var renderer in renderers)
            {
                Bounds bounds = renderer.bounds;
                float projectedExtent = Vector3.Dot(new Vector3(Mathf.Abs(up.x), Mathf.Abs(up.y), Mathf.Abs(up.z)), bounds.extents);
                minimum = Mathf.Min(minimum, Vector3.Dot(bounds.center, up) - projectedExtent);
            }
            float surface = Vector3.Dot(board.GetAnchor(0).position, up);
            float worldClearance = card.BoardClearance * board.transform.TransformVector(Vector3.up).magnitude;
            Assert.GreaterOrEqual(minimum, surface + worldClearance - 0.002f);
            Camera camera = Camera.main;
            Assert.NotNull(camera);
            Vector3 toCamera = Vector3.ProjectOnPlane(camera.transform.position - card.transform.position, up).normalized;
            Assert.Greater(Vector3.Dot(-card.transform.forward, toCamera), 0.98f);
            Assert.Greater(card.transform.Find("Model").GetComponentInChildren<Renderer>(true).bounds.size.magnitude, 0.01f);
        }
    }
}
#endif
