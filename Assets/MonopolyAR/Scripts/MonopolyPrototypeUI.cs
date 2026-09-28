using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MonopolyAR
{
    public sealed class MonopolyPrototypeUI : MonoBehaviour
    {
        public MonopolyTurnManager game;
        public Camera boardCamera;
        public ARBoardPlacement arPlacement;
        private Text placementText;
        public Button RollButton { get; private set; }
        public Button EndTurnButton { get; private set; }
        public Text StatusText { get; private set; }
        public Text ResultsText { get; private set; }
        public bool ShowTileNumbers { get; private set; } = true;
        private RectTransform safeRoot;
        private RectTransform numberLayer;
        private Text[] numbers;
        private Font font;
        public Toggle NumbersToggle { get; private set; }
        private Canvas uiCanvas;
        private int lastWidth, lastHeight;
        private Rect lastSafeArea;
        private float lastScaleFactor;


        private bool landscape;

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("PrototypeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            uiCanvas = canvas;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800);
            scaler.matchWidthOrHeight = 0.5f;
            numberLayer = Rect("TileNumbers", canvasObject.transform, Vector2.zero, Vector2.one);
            safeRoot = Rect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.one);
            var panel = Rect("StatusPanel", safeRoot, new Vector2(0, 1), new Vector2(0, 1));
            Place(panel, 12, 12, 304, 372);
            panel.gameObject.AddComponent<Image>().color = new Color(.22f, .22f, .22f, 1);
            StatusText = Label("Player", panel, 22, TextAnchor.UpperLeft);
            Place(StatusText.rectTransform, 16, 16, 272, 56);
            ResultsText = Label("DiceResults", panel, 22, TextAnchor.UpperLeft);
            Place(ResultsText.rectTransform, 16, 88, 272, 84);
            var buttonGray = new Color(.8f, .8f, .8f, 1);
            RollButton = Button("RollDice", "Lanzar dados", panel, Vector2.zero, Vector2.one, buttonGray);
            Place(RollButton.GetComponent<RectTransform>(), 16, 192, 272, 48);
            EndTurnButton = Button("EndTurn", "Finalizar turno", panel, Vector2.zero, Vector2.one, buttonGray);
            Place(EndTurnButton.GetComponent<RectTransform>(), 16, 252, 272, 48);
            RollButton.onClick.AddListener(() => { if (!arPlacement || arPlacement.BoardPlaced) game.TryRoll(); });
            EndTurnButton.onClick.AddListener(() => { if (!arPlacement || arPlacement.BoardPlaced) game.TryEndTurn(); });
            var toggleRect = Rect("ToggleNumbers", panel, Vector2.zero, Vector2.one);
            Place(toggleRect, 16, 322, 272, 32);
            NumbersToggle = toggleRect.gameObject.AddComponent<Toggle>();
            var box = Rect("Background", toggleRect, Vector2.zero, Vector2.one);
            Place(box, 0, 2, 28, 28);
            var boxImage = box.gameObject.AddComponent<Image>();
            boxImage.color = new Color(.9f, .9f, .9f, 1);
            var mark = Label("Checkmark", box, 22, TextAnchor.MiddleCenter);
            mark.text = "X"; mark.color = Color.black;
            NumbersToggle.targetGraphic = boxImage;
            NumbersToggle.graphic = mark;
            var toggleLabel = Label("Label", toggleRect, 22, TextAnchor.MiddleLeft);
            Place(toggleLabel.rectTransform, 38, 0, 234, 32);
            toggleLabel.text = "Mostrar números";
            toggleLabel.raycastTarget = true;
            NumbersToggle.SetIsOnWithoutNotify(ShowTileNumbers);
            NumbersToggle.onValueChanged.AddListener(value =>
            {
                ShowTileNumbers = value;
                numberLayer.gameObject.SetActive(value);
            });
            if (arPlacement)
            {
                placementText = Label("PlacementMessage", canvasObject.transform, 20, TextAnchor.LowerCenter);
                placementText.rectTransform.anchorMin = new Vector2(.28f, 0);
                placementText.rectTransform.anchorMax = new Vector2(.98f, .16f);
            }
            if (!FindObjectOfType<EventSystem>()) new GameObject("PrototypeEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform, false);
            numbers = new Text[MonopolyBoard.TileCount];
            for (int i = 0; i < numbers.Length; i++)
            {
                numbers[i] = Label($"TileNumber_{i:00}", numberLayer, 17, TextAnchor.MiddleCenter);
                numbers[i].text = i.ToString("00"); numbers[i].color = Color.white;
                numbers[i].gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .95f);
                numbers[i].rectTransform.anchorMin = numbers[i].rectTransform.anchorMax = new Vector2(.5f, .5f);
                numbers[i].rectTransform.sizeDelta = new Vector2(38, 28);
            }
        }

        private void LateUpdate()
        {
            if (!safeRoot || !game) return;
            Rect safe = Screen.safeArea;
            safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            if (!arPlacement && boardCamera && (lastWidth != Screen.width || lastHeight != Screen.height || lastSafeArea != safe || !Mathf.Approximately(lastScaleFactor, uiCanvas.scaleFactor)))
            {
                lastWidth = Screen.width; lastHeight = Screen.height; lastSafeArea = safe;
                float scale = uiCanvas.scaleFactor;
                lastScaleFactor = scale;
                landscape = Screen.width > Screen.height * 1.25f;
                if (landscape)
                {
                    boardCamera.rect = new Rect((safe.xMin + 328 * scale) / Screen.width, (safe.yMin + 8 * scale) / Screen.height,
                        Mathf.Max(40 * scale, safe.width - 336 * scale) / Screen.width, (safe.height - 16 * scale) / Screen.height);
                }
                else
                {
                    float bottom = safe.yMin + 163 * scale;
                    float top = safe.yMax - 120 * scale;
                    boardCamera.rect = new Rect((safe.xMin + 8 * scale) / Screen.width, bottom / Screen.height,
                        (safe.width - 16 * scale) / Screen.width, Mathf.Max(40 * scale, top - bottom) / Screen.height);
                }
                FitBoardToCamera();
            }
            bool placed = !arPlacement || arPlacement.BoardPlaced;
            if (placementText) { placementText.text = arPlacement.PlacementMessage; placementText.gameObject.SetActive(!placed); }
            numberLayer.gameObject.SetActive(placed && ShowTileNumbers);
            bool ready = game.Phase != TurnPhase.NotReady && game.Phase != TurnPhase.Error;
            StatusText.text = !placed ? "Jugador: Empresario\nCasilla: 00" : ready ? $"Jugador: {game.ActivePlayer.displayName}\nCasilla: {game.ActivePlayer.CurrentTile:00}" : (game.Phase == TurnPhase.Error ? game.ErrorMessage : "Preparando partida...");
            ResultsText.text = game.dice.HasResult ? $"Dado 1: {game.dice.DieOne}\nDado 2: {game.dice.DieTwo}\nTotal: {game.dice.Sum}" : "Dado 1: -\nDado 2: -\nTotal: -";
            RollButton.interactable = placed && game.CanRoll;
            EndTurnButton.interactable = placed && game.CanEndTurn;
            if (placed && ready && boardCamera && ShowTileNumbers)
                for (int i = 0; i < numbers.Length; i++)
                {
                    Vector3 world = game.board.GetAnchor(i).position + game.board.transform.TransformVector(Vector3.up * .08f);
                    Vector3 screen = boardCamera.WorldToScreenPoint(world);
                    numbers[i].gameObject.SetActive(screen.z > 0 && screen.x >= 0 && screen.x <= Screen.width && screen.y >= 0 && screen.y <= Screen.height);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(numberLayer, screen, null, out Vector2 local);
                    numbers[i].rectTransform.anchoredPosition = local;
                }
        }

        private void FitBoardToCamera()
        {
            var renderers = game.board.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            // Leave room above the original board for the standing tokens.
            bounds.Encapsulate(bounds.center + Vector3.up * 2.4f);
            Vector3 center = bounds.center;
            boardCamera.transform.position = center - boardCamera.transform.forward * 32;
            float vertical = 0, horizontal = 0;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 point = center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                        Vector3 local = boardCamera.transform.InverseTransformPoint(point);
                        horizontal = Mathf.Max(horizontal, Mathf.Abs(local.x));
                        vertical = Mathf.Max(vertical, Mathf.Abs(local.y));
                    }
            boardCamera.orthographic = true;
            boardCamera.orthographicSize = Mathf.Max(vertical, horizontal / boardCamera.aspect) * 1.06f;
        }

        private static void Place(RectTransform r, float left, float top, float width, float height)
        {
            SetRect(r, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(left, -top - height), new Vector2(left + width, -top));
        }
        private RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            result.SetParent(parent, false); SetRect(result, min, max, Vector2.zero, Vector2.zero); return result;
        }
        private static void SetRect(RectTransform r, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        { r.anchorMin = min; r.anchorMax = max; r.offsetMin = offsetMin; r.offsetMax = offsetMax; }
        private Text Label(string name, Transform parent, int size, TextAnchor alignment)
        {
            var r = Rect(name, parent, Vector2.zero, Vector2.one); var label = r.gameObject.AddComponent<Text>();
            label.font = font; label.fontSize = size; label.color = Color.white; label.alignment = alignment;
            label.raycastTarget = false; label.resizeTextForBestFit = true; label.resizeTextMinSize = 11; label.resizeTextMaxSize = size;
            return label;
        }
        private Button Button(string name, string caption, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var r = Rect(name, parent, min, max); var image = r.gameObject.AddComponent<Image>(); image.color = color;
            var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.disabledColor = new Color(.65f, .65f, .65f, 1); colors.fadeDuration = 0; button.colors = colors;
            var text = Label("Label", r, 22, TextAnchor.MiddleCenter); text.text = caption; text.color = Color.black; return button;
        }
    }
}
