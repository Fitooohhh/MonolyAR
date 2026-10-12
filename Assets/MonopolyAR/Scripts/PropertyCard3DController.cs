using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace MonopolyAR
{
    public sealed class PropertyCard3DController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Renderer colorHeaderRenderer;
        [SerializeField] private Canvas worldCanvas;
        [SerializeField] private GraphicRaycaster worldRaycaster;
        [SerializeField] private Button closeButton;
        [SerializeField] private MonopolyBoard board;
        [SerializeField] private Camera targetCamera;
        private SpecialCard3DController specialCard;
        [Header("Text")]
        [SerializeField] private Text groupText;
        [SerializeField] private Text propertyNameText;
        [SerializeField] private Text priceText;
        [SerializeField] private Text rentTitleText;
        [SerializeField] private Text baseRentText;
        [SerializeField] private Text rent1HouseText;
        [SerializeField] private Text rent2HousesText;
        [SerializeField] private Text rent3HousesText;
        [SerializeField] private Text rent4HousesText;
        [SerializeField] private Text hotelRentText;
        [SerializeField] private Text housePriceText;
        [SerializeField] private Text hotelPriceText;
        [Header("Placement")]
        // BoardRoot is placed at roughly 0.6 / 14 in AR.  At the old 0.22
        // scale the card was only a few centimetres tall, so it was easy to
        // miss on a phone.  This keeps it comparable to a player token while
        // remaining much smaller than the board.
        [SerializeField] private float modelScale = 0.75f;
        [SerializeField] private Vector3 boardLocalPosition = new Vector3(0f, 1.70f, 1.15f);
        [SerializeField] private float targetWorldHeight = 0.22f;
        [SerializeField] private float minimumWorldHeight = 0.20f;
        [SerializeField] private float maximumWorldHeight = 0.24f;
        [SerializeField] private float minimumViewportHeight = 0.08f;
        [SerializeField] private float showDuration = 0.55f;
        [SerializeField] private float liftDistance = 0.025f;
        [Header("Dice rest position")]
        [SerializeField] private float diceRestLateral = 0.95f;
        [SerializeField] private float diceRestForward = 0.35f;
        [SerializeField] private float diceMoveDuration = 0.4f;
        private Coroutine diceMoveRoutine;
        private Coroutine diceRestoreRoutine;
        private MonopolyDice savedDice;
        private Transform savedDieOneParent;
        private Transform savedDieTwoParent;
        private Vector3 savedDieOneLocalPosition;
        private Vector3 savedDieTwoLocalPosition;
        private Quaternion savedDieOneLocalRotation;
        private Quaternion savedDieTwoLocalRotation;
        private bool diceStateSaved;
        [SerializeField] private float boardClearance = 0.015f;
        [SerializeField] private float minimumWorldClearance = 0.006f;

        private readonly Dictionary<MonopolyPlayer, bool> subscribedPlayers = new Dictionary<MonopolyPlayer, bool>();
        private MonopolyTurnManager turnManager;
        private Coroutine animationRoutine;
        private Vector3 restingPosition;
        private Quaternion restingRotation;
        private Vector3 restingLocalScale;
        private float resolvedWorldHeight;
        private readonly List<TextMeshPro> frontText3D = new List<TextMeshPro>();
        private Transform frontContent;
        private bool specialPresentation;
        private string specialTitle;
        private string specialSymbol;
        private string specialSummary;
        private Color specialHeaderColor;

        public bool IsOpen { get; private set; }
        public MonopolySpaceData CurrentData { get; private set; }
        public bool PurchaseDecisionPending { get; private set; }
        public bool RentDecisionPending { get; private set; }
        public int RentAmount { get; private set; }
        public int RentPaid { get; private set; }
        public int RentDebt { get; private set; }
        public MonopolyPlayer RentOwner { get; private set; }
        public string RentSummary => RentDecisionPending ? $"ALQUILER: ${RentAmount} | PAGADO: ${RentPaid} | DEUDA: ${RentDebt}" : string.Empty;
        public MonopolyPlayer CurrentOwner => board ? board.GetOwner(CurrentData.index) : null;
        public bool CanPurchaseCurrent => PurchaseDecisionPending && CurrentOwner == null && CurrentPlayer && CurrentPlayer.Money >= CurrentData.purchasePrice;
        public MonopolyPlayer CurrentPlayer { get; private set; }
        public float ModelScale => modelScale;
        public float BoardClearance => boardClearance;
        public Renderer ColorHeaderRenderer => colorHeaderRenderer;
        public bool LastPresentationValid { get; private set; }
        public string LastPresentationDiagnostics { get; private set; }
        public float TargetWorldHeight => resolvedWorldHeight > 0f ? resolvedWorldHeight : targetWorldHeight;

        private void Awake()
        {
            if (closeButton) closeButton.onClick.AddListener(Close);
            if (modelRoot) modelRoot.localScale = Vector3.one * modelScale;
            if (!worldRaycaster && worldCanvas) worldRaycaster = worldCanvas.GetComponent<GraphicRaycaster>();
            if (worldCanvas) worldCanvas.enabled = false;
            if (worldRaycaster) worldRaycaster.enabled = false;
            if (worldCanvas) worldCanvas.gameObject.SetActive(false);
            if (modelRoot) modelRoot.gameObject.SetActive(false);
            BuildFrontContent();
        }

        private void BuildFrontContent()
        {
            if (frontContent) return;
            var root = new GameObject("FrontContent");
            frontContent = root.transform;
            frontContent.SetParent(transform, false);
            frontContent.localRotation = Quaternion.identity;
            frontContent.localPosition = new Vector3(0f, 0f, -0.075f);
            string[] labels = { "Group", "Property", "Price", "BaseRent", "Rent1", "Rent2", "Rent3", "Rent4", "HotelRent", "HousePrice", "HotelPrice" };
            for (int i = 0; i < labels.Length; i++)
            {
                var text = new GameObject(labels[i]).AddComponent<TextMeshPro>();
                text.transform.SetParent(frontContent, false);
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color(0.04f, 0.035f, 0.025f, 1f);
                text.fontSize = i == 1 ? 1.44f : (i == 2 ? 1.38f : (i == 0 ? 1.0f : 0.92f));
                text.enableWordWrapping = false;
                text.rectTransform.sizeDelta = new Vector2(1.12f, 0.13f);
                // CardBody local vertical bounds are approximately 0.09..1.97.
                // Keep all twelve rows inside that measured surface.
                text.transform.localPosition = new Vector3(0f, 1.50f - i * 0.13f, 0f);
                frontText3D.Add(text);
            }
            frontContent.gameObject.SetActive(false);
            foreach (var legacy in GetComponentsInChildren<Text>(true)) legacy.gameObject.SetActive(false);
            if (closeButton) closeButton.gameObject.SetActive(false);
        }

        private void UpdateFrontContent(MonopolySpaceData data)
        {
            if (frontText3D.Count == 0) BuildFrontContent();
            string[] values;
            if (specialPresentation)
            {
                values = new string[11];
                values[0] = specialTitle.ToUpperInvariant();
                values[1] = specialSymbol;
                values[2] = data.displayName.ToUpperInvariant();
                string[] bodyLines = (specialSummary ?? string.Empty).Replace("\r", string.Empty).Split('\n');
                for (int i = 0; i < 8; i++)
                    values[3 + i] = i < bodyLines.Length ? bodyLines[i].ToUpperInvariant() : string.Empty;
            }
            else
            {
                values = new[] {
                    RentDecisionPending ? $"ALQUILER ${RentAmount} · {PlayerName(RentOwner).ToUpperInvariant()}" : (data.colorGroup == PropertyColorGroup.None ? "PROPIEDAD" : data.colorGroup.ToString().ToUpperInvariant()) + (CurrentOwner ? " · DUEÑO: " + CurrentOwner.displayName.ToUpperInvariant() : ""),
                    data.displayName.ToUpperInvariant(),
                    "PRECIO $" + data.purchasePrice,
                    "ALQUILER BASE   $" + data.baseRent,
                    "1 CASA   $" + data.rent1House,
                    "2 CASAS   $" + data.rent2Houses,
                    "3 CASAS   $" + data.rent3Houses,
                    "4 CASAS   $" + data.rent4Houses,
                    "HOTEL   $" + data.rentHotel,
                    "PRECIO CASA   $" + data.housePrice,
                    "PRECIO HOTEL   $" + data.hotelPrice
                };
            }
            for (int i = 0; i < frontText3D.Count; i++) frontText3D[i].text = values[i];
            frontContent.gameObject.SetActive(true);
        }


        private void Start()
        {
            if (!board) board = GetComponentInParent<MonopolyBoard>() ?? FindObjectOfType<MonopolyBoard>();
            if (!targetCamera) targetCamera = Camera.main;
            turnManager = FindObjectOfType<MonopolyTurnManager>();
            specialCard = FindObjectOfType<SpecialCard3DController>(true);
            SubscribeToPlayers();
            CloseInternal("inicialización", false);
        }

        private void SubscribeToPlayers()
        {
            if (!turnManager || turnManager.ConfiguredPlayers == null) return;
            foreach (var player in turnManager.ConfiguredPlayers)
            {
                if (!player || subscribedPlayers.ContainsKey(player)) continue;
                player.ArrivedAtSpace += OnArrivedAtSpace;
                subscribedPlayers.Add(player, true);
            }
        }

        private void OnArrivedAtSpace(MonopolyPlayer player, MonopolySpaceData data)
        {
            if (data.type == SpaceType.Property || data.type == SpaceType.Station || data.type == SpaceType.Utility)
            {
                ResolveRentForArrival(player, data);
                Debug.Log($"[PropertyCard3D] ArrivedAtSpace: jugador={PlayerName(player)}, índice={data.index}, tipo={data.type}.", this);
                Show(player, data);
            }
            else
            {
                CloseInternal($"casilla {data.index} no es comprable", false);
                specialCard = specialCard ? specialCard : FindObjectOfType<SpecialCard3DController>(true);
                if (specialCard && data.type != SpaceType.Start) specialCard.Show(player, data);
            }
        }

        private void ResolveRentForArrival(MonopolyPlayer player, MonopolySpaceData data)
        {
            RentDecisionPending = false;
            RentAmount = RentPaid = RentDebt = 0;
            RentOwner = null;
            Debug.Log($"[PropertyCard3D] Resolviendo alquiler: casilla={data.index}, dueño={(board && board.GetOwner(data.index) ? board.GetOwner(data.index).displayName : "ninguno")}, dinero={player.Money}, rentaBase={data.baseRent}.", this);
            if (!board || !turnManager || board.GetOwner(data.index) == null || board.GetOwner(data.index) == player) return;
            if (board.TryCollectRent(player, data.index, turnManager.dice ? turnManager.dice.Sum : 0,
                out MonopolyPlayer owner, out int rent, out int paid, out int debt, out string reason))
            {
                RentOwner = owner; RentAmount = rent; RentPaid = paid; RentDebt = debt; RentDecisionPending = true;
                Debug.Log($"[PropertyCard3D] Alquiler resuelto: propiedad={data.displayName}, propietario={PlayerName(owner)}, importe=${rent}, pagado=${paid}, deuda=${debt}. {reason}", this);
            }
        }

        public void Show(MonopolyPlayer player, MonopolySpaceData data)
        {
            if (data.type != SpaceType.Property && data.type != SpaceType.Station && data.type != SpaceType.Utility) { CloseInternal($"casilla {data.index} no es comprable", false); return; }
            specialPresentation = false;
            PurchaseDecisionPending = board && board.GetOwner(data.index) == null;
            ShowInternal(player, data);
        }

        public bool ShowSpecial(MonopolyPlayer player, MonopolySpaceData data, string title, string symbol, string summary, Color headerColor)
        {
            specialPresentation = true;
            specialTitle = title ?? string.Empty;
            specialSymbol = symbol ?? string.Empty;
            specialSummary = summary ?? string.Empty;
            specialHeaderColor = headerColor;
            PurchaseDecisionPending = false;
            RentDecisionPending = false;
            RentAmount = RentPaid = RentDebt = 0;
            RentOwner = null;
            bool shown = ShowInternal(player, data);
            if (!shown) specialPresentation = false;
            return shown;
        }

        public void UpdateSpecialSummary(string summary)
        {
            specialSummary = summary ?? string.Empty;
            if (specialPresentation && IsOpen) UpdateFrontContent(CurrentData);
        }

        private bool ShowInternal(MonopolyPlayer player, MonopolySpaceData data)
        {
            Debug.Log($"[PropertyCard3D] Apertura solicitada: jugador={PlayerName(player)}, propiedad={data.index} ({data.displayName}).", this);
            if (!board) board = GetComponentInParent<MonopolyBoard>() ?? FindObjectOfType<MonopolyBoard>();
            if (!targetCamera) targetCamera = Camera.main;
            turnManager = turnManager ? turnManager : FindObjectOfType<MonopolyTurnManager>();
            if (!board || !targetCamera || !modelRoot || !worldCanvas || !colorHeaderRenderer)
            {
                FailPresentation($"referencias incompletas: board={!!board}, camera={!!targetCamera}, model={!!modelRoot}, canvas={!!worldCanvas}, header={!!colorHeaderRenderer}");
                return false;
            }
            CurrentPlayer = player;
            CurrentData = data;
            if (!specialPresentation)
                PurchaseDecisionPending = board.GetOwner(data.index) == null && !RentDecisionPending;
            UpdateTexts(data);
            UpdateFrontContent(data);
            if (specialPresentation) ApplyHeaderColor(specialHeaderColor);
            else ApplyHeaderColor(data.colorGroup);
            // Bounds are unavailable while the imported model is inactive.
            // Activate it before placement so the base can be aligned to the
            // real board plane instead of intersecting the board.
            if (modelRoot)
            {
                modelRoot.gameObject.SetActive(true);
                modelRoot.localScale = Vector3.one * modelScale;
            }
            if (worldCanvas)
            {
                ConfigureDesignCanvas();
                worldCanvas.gameObject.SetActive(true);
                worldCanvas.enabled = true;
                worldCanvas.worldCamera = targetCamera;
                // Keep depth testing with the 3D card. A high override sorting
                // order makes text render over the entire AR scene.
                worldCanvas.overrideSorting = false;
                worldCanvas.sortingOrder = 0;
            }
            // Android/XR can report an invalid raycast viewport for a world
            // canvas. Keep the raycaster configured but use the stable
            // projected-button hit path below; this still gives the 3D button
            // a valid touch area without allowing a bad XR viewport to poison
            // the EventSystem.
            if (worldRaycaster) worldRaycaster.enabled = false;
            foreach (var renderer in modelRoot.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                renderer.forceRenderingOff = false;
            }
            Debug.Log($"[PropertyCard3D] Prefab activado: active={gameObject.activeInHierarchy}, model={modelRoot.gameObject.activeInHierarchy}, renderers={RendererSummary()}.", this);
            PlaceForBoardAndCamera();
            if (!ValidatePresentation(out string diagnostics))
            {
                FailPresentation(diagnostics);
                return false;
            }
            LastPresentationValid = true;
            LastPresentationDiagnostics = diagnostics;
            IsOpen = true;
            Debug.Log($"[PropertyCard3D] Presentación válida y turno bloqueado. {diagnostics}", this);
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            if (diceMoveRoutine != null) StopCoroutine(diceMoveRoutine);
            SaveDiceStateBeforeMoving();
            diceMoveRoutine = StartCoroutine(MoveDiceAside());
            animationRoutine = StartCoroutine(AnimateOpen());
            return true;
        }

        public void AcknowledgeRent()
        {
            if (!RentDecisionPending) return;
            Debug.Log($"[PropertyCard3D] Alquiler entendido: propiedad={CurrentData.displayName}, pagado=${RentPaid}, deuda=${RentDebt}.", this);
            CloseInternal("ENTENDIDO", true);
        }

        public bool PurchaseCurrent()
        {
            if (!PurchaseDecisionPending || !CurrentPlayer || !board) return false;
            if (!board.TryPurchase(CurrentPlayer, CurrentData.index, out string reason))
            {
                Debug.Log($"[PropertyCard3D] Compra rechazada: {reason}", this);
                return false;
            }
            PurchaseDecisionPending = false;
            UpdateFrontContent(CurrentData);
            Debug.Log($"[PropertyCard3D] Compra realizada: {CurrentPlayer.displayName} -> {CurrentData.displayName} por ${CurrentData.purchasePrice}.", this);
            CloseInternal("COMPRAR", true);
            return true;
        }

        public void DeclinePurchase()
        {
            if (!PurchaseDecisionPending) return;
            PurchaseDecisionPending = false;
            Debug.Log($"[PropertyCard3D] Compra rechazada por el jugador: {CurrentData.displayName}.", this);
            CloseInternal("NO COMPRAR", true);
        }

        private IEnumerator MoveDiceAside()
        {
            MonopolyDice dice = FindObjectOfType<MonopolyDice>(true);
            if (!dice || dice.IsRolling || !dice.dieOneVisual || !dice.dieTwoVisual)
                yield break;

            Vector3 up = board ? board.transform.up.normalized : Vector3.up;
            Vector3 right = board ? board.transform.right.normalized : Vector3.right;
            Vector3 forward = board ? board.transform.forward.normalized : Vector3.forward;
            Vector3 center = transform.position + forward * diceRestForward;
            Vector3[] starts = { dice.dieOneVisual.position, dice.dieTwoVisual.position };
            Vector3[] targets = { center - right * diceRestLateral, center + right * diceRestLateral };
            for (int i = 0; i < targets.Length; i++)
                targets[i] = RestOnBoard(dice, i == 0 ? dice.dieOneVisual : dice.dieTwoVisual, targets[i], up);

            Debug.Log($"[PropertyCard3D] Apartando dados: d1={targets[0]:F3}, d2={targets[1]:F3}, duración={diceMoveDuration:F2}s.", this);
            float elapsed = 0f;
            while (elapsed < diceMoveDuration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(0.01f, diceMoveDuration));
                dice.dieOneVisual.position = Vector3.Lerp(starts[0], targets[0], t);
                dice.dieTwoVisual.position = Vector3.Lerp(starts[1], targets[1], t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            dice.dieOneVisual.position = targets[0];
            dice.dieTwoVisual.position = targets[1];
            Debug.Log("[PropertyCard3D] Dados en posición de descanso; resultados y rotaciones conservados.", this);
            diceMoveRoutine = null;
        }

        private void SaveDiceStateBeforeMoving()
        {
            if (diceStateSaved) return;
            MonopolyDice dice = FindObjectOfType<MonopolyDice>(true);
            if (!dice || dice.IsRolling || !dice.dieOneVisual || !dice.dieTwoVisual) return;
            savedDice = dice;
            savedDieOneParent = dice.dieOneVisual.parent;
            savedDieTwoParent = dice.dieTwoVisual.parent;
            savedDieOneLocalPosition = dice.dieOneVisual.localPosition;
            savedDieTwoLocalPosition = dice.dieTwoVisual.localPosition;
            savedDieOneLocalRotation = dice.dieOneVisual.localRotation;
            savedDieTwoLocalRotation = dice.dieTwoVisual.localRotation;
            diceStateSaved = true;
            Debug.Log($"[PropertyCard3D] Posiciones originales de dados guardadas: d1={savedDieOneLocalPosition:F3}, d2={savedDieTwoLocalPosition:F3}.", this);
        }

        private IEnumerator RestoreDiceState()
        {
            if (!diceStateSaved || !savedDice || !savedDice.dieOneVisual || !savedDice.dieTwoVisual)
                yield break;
            if (diceMoveRoutine != null)
            {
                StopCoroutine(diceMoveRoutine);
                diceMoveRoutine = null;
            }
            Transform one = savedDice.dieOneVisual;
            Transform two = savedDice.dieTwoVisual;
            one.SetParent(savedDieOneParent, true);
            two.SetParent(savedDieTwoParent, true);
            Vector3 startOne = one.localPosition;
            Vector3 startTwo = two.localPosition;
            Quaternion rotOne = one.localRotation;
            Quaternion rotTwo = two.localRotation;
            float elapsed = 0f;
            while (elapsed < diceMoveDuration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(0.01f, diceMoveDuration));
                one.localPosition = Vector3.Lerp(startOne, savedDieOneLocalPosition, t);
                two.localPosition = Vector3.Lerp(startTwo, savedDieTwoLocalPosition, t);
                one.localRotation = Quaternion.Slerp(rotOne, savedDieOneLocalRotation, t);
                two.localRotation = Quaternion.Slerp(rotTwo, savedDieTwoLocalRotation, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            one.localPosition = savedDieOneLocalPosition;
            two.localPosition = savedDieTwoLocalPosition;
            one.localRotation = savedDieOneLocalRotation;
            two.localRotation = savedDieTwoLocalRotation;
            diceStateSaved = false;
            savedDice = null;
            diceRestoreRoutine = null;
            Debug.Log("[PropertyCard3D] Dados restaurados a sus posiciones originales.", this);
        }

        private void RestoreDiceIfNeeded()
        {
            if (!diceStateSaved || diceRestoreRoutine != null) return;
            diceRestoreRoutine = StartCoroutine(RestoreDiceState());
        }

        private Vector3 RestOnBoard(MonopolyDice dice, Transform die, Vector3 target, Vector3 up)
        {
            Vector3 delta = target - die.position;
            die.position = target;
            var renderers = die.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            float minimum = 0f;
            foreach (var renderer in renderers)
            {
                if (!renderer || !renderer.enabled) continue;
                Bounds b = renderer.bounds;
                float extent = Vector3.Dot(new Vector3(Mathf.Abs(up.x), Mathf.Abs(up.y), Mathf.Abs(up.z)), b.extents);
                float candidate = Vector3.Dot(b.center, up) - extent;
                if (!hasBounds || candidate < minimum) minimum = candidate;
                hasBounds = true;
            }
            if (hasBounds && board)
            {
                Transform anchor = board.GetAnchor(0);
                float surface = anchor ? Vector3.Dot(anchor.position, up) : Vector3.Dot(board.transform.position, up);
                float clearance = dice.SurfaceClearance * board.transform.TransformVector(Vector3.up).magnitude;
                target += up * (surface + clearance - minimum);
            }
            die.position = target;
            return target;
        }

        public void Close()
        {
            CloseInternal("CERRAR", true);
        }

        public void Hide()
        {
            Close();
        }

        private void CloseInternal(string reason, bool writeLog)
        {
            bool wasOpen = IsOpen;
            IsOpen = false;
            PurchaseDecisionPending = false;
            RentDecisionPending = false;
            CurrentPlayer = null;
            specialPresentation = false;
            specialTitle = specialSymbol = specialSummary = string.Empty;
            RestoreDiceIfNeeded();
            if (animationRoutine != null) { StopCoroutine(animationRoutine); animationRoutine = null; }
            transform.localScale = Vector3.one;
            if (modelRoot) modelRoot.localScale = Vector3.one * modelScale;
            if (modelRoot) modelRoot.gameObject.SetActive(false);
            if (frontContent) frontContent.gameObject.SetActive(false);
            if (worldRaycaster) worldRaycaster.enabled = false;
            if (worldCanvas) worldCanvas.enabled = false;
            if (worldCanvas) worldCanvas.gameObject.SetActive(false);
            if (writeLog || wasOpen)
                Debug.Log($"[PropertyCard3D] Cerrada ({reason}). IsOpen=false; turno desbloqueado; CanEndTurn={(turnManager ? turnManager.CanEndTurn : false)}.", this);
        }

        private void PlaceForBoardAndCamera()
        {
            transform.SetParent(board.transform, false);
            transform.localScale = Vector3.one;
            transform.localPosition = boardLocalPosition;
            if (!targetCamera) targetCamera = Camera.main;
            Vector3 toCamera = targetCamera ? targetCamera.transform.position - transform.position : -board.transform.forward;
            toCamera = Vector3.ProjectOnPlane(toCamera, board.transform.up);
            if (toCamera.sqrMagnitude < 0.0001f) toCamera = -board.transform.forward;
            // The imported Blender front (+Y) arrives as Unity -Z after FBX axis conversion.
            restingRotation = Quaternion.LookRotation(-toCamera.normalized, board.transform.up);
            transform.rotation = restingRotation;
            ScaleToWorldHeight();
            AlignCanvasToCardSurface();
            AlignBaseToBoardSurface();
            restingPosition = transform.position;
            restingLocalScale = transform.localScale;
        }

        private void ScaleToWorldHeight()
        {
            if (!TryGetModelBounds(out Bounds bounds)) return;
            Vector3 up = board.transform.up.normalized;
            float currentHeight = ProjectedSize(bounds, up);
            if (currentHeight <= 0.00001f) return;
            resolvedWorldHeight = ResolveWorldHeight();
            float ratio = Mathf.Clamp(resolvedWorldHeight / currentHeight, 0.25f, 40f);
            transform.localScale *= ratio;
        }

        private float ResolveWorldHeight()
        {
            float boardWidth = 0.6f;
            if (board && board.Anchors != null && board.Anchors.Length > 1)
            {
                Vector3 first = board.transform.InverseTransformPoint(board.Anchors[0].position);
                float minX = first.x, maxX = first.x, minZ = first.z, maxZ = first.z;
                foreach (Transform anchor in board.Anchors)
                {
                    if (!anchor) continue;
                    Vector3 local = board.transform.InverseTransformPoint(anchor.position);
                    minX = Mathf.Min(minX, local.x); maxX = Mathf.Max(maxX, local.x);
                    minZ = Mathf.Min(minZ, local.z); maxZ = Mathf.Max(maxZ, local.z);
                }
                boardWidth = Mathf.Max((maxX - minX) * board.transform.lossyScale.x,
                    (maxZ - minZ) * board.transform.lossyScale.z);
            }
            float distance = targetCamera ? Vector3.Distance(targetCamera.transform.position, transform.position) : 0.7f;
            float distanceFactor = Mathf.Clamp(distance / 0.7f, 0.85f, 1.15f);
            return Mathf.Clamp(Mathf.Max(targetWorldHeight, boardWidth * 0.36f) * distanceFactor,
                minimumWorldHeight, maximumWorldHeight);
        }

        private void AlignCanvasToCardSurface()
        {
            if (!worldCanvas || !modelRoot) return;
            Transform cardBody = modelRoot.Find("TarjetaPropiedad/CardBody") ?? modelRoot.Find("CardBody");
            if (!cardBody) return;
            Renderer[] bodyRenderers = cardBody.GetComponentsInChildren<Renderer>(true);
            if (bodyRenderers.Length == 0) return;

            bool hasBounds = false;
            Bounds localBounds = default;
            foreach (Renderer renderer in bodyRenderers)
            {
                if (!renderer || renderer.bounds.size.sqrMagnitude <= 0.0000001f) continue;
                Bounds worldBounds = renderer.bounds;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 point = transform.InverseTransformPoint(worldBounds.center + Vector3.Scale(worldBounds.extents, new Vector3(x, y, z)));
                            if (!hasBounds) { localBounds = new Bounds(point, Vector3.zero); hasBounds = true; }
                            else localBounds.Encapsulate(point);
                        }
            }
            if (!hasBounds) return;
            RectTransform canvasRect = worldCanvas.transform as RectTransform;
            if (!canvasRect) return;
            Vector2 canvasSize = canvasRect.sizeDelta;
            // RectTransform geometry is expressed directly in canvas units;
            // CanvasScaler controls text rasterisation, not the world-space
            // RectTransform dimensions. Derive the uniform scale from the
            // measured CardBody bounds so the complete design surface fits.
            Vector2 physicalCanvasSize = canvasSize;
            float widthScale = Mathf.Max(0.0001f, localBounds.size.x * 0.88f / Mathf.Max(0.001f, physicalCanvasSize.x));
            float heightScale = Mathf.Max(0.0001f, localBounds.size.y * 0.86f / Mathf.Max(0.001f, physicalCanvasSize.y));
            float localScale = Mathf.Min(widthScale, heightScale);
            canvasRect.localScale = Vector3.one * localScale;
            float depthOffset = 0.0025f / Mathf.Max(0.0001f, transform.lossyScale.x);
            canvasRect.localPosition = new Vector3(localBounds.center.x, localBounds.center.y, localBounds.min.z - depthOffset);
            // UI graphics are front-faced along the imported card surface;
            // the previous 180-degree flip made legacy UI.Text back-face
            // culled on Android even though the Canvas bounds were valid.
            canvasRect.localRotation = Quaternion.identity;
            Debug.Log($"[PropertyCard3D] Canvas alineado a CardBody: localSize={localBounds.size:F4}, canvasScale={localScale:F5}, localPos={canvasRect.localPosition:F4}, frontOffset={depthOffset:F4}.", this);
        }

        private void ConfigureDesignCanvas()
        {
            if (!worldCanvas) return;
            var rect = worldCanvas.transform as RectTransform;
            if (rect)
            {
                // Compact design space: the physical CardBody is unchanged,
                // but each UI unit maps to a larger world-space glyph.
                rect.sizeDelta = new Vector2(340f, 500f);
                rect.localScale = Vector3.one;
                rect.localPosition = Vector3.zero;
                rect.localRotation = Quaternion.identity;
            }
            worldCanvas.overrideSorting = false;
            worldCanvas.sortingOrder = 0;
            var scaler = worldCanvas.GetComponent<CanvasScaler>();
            if (scaler)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
                scaler.referencePixelsPerUnit = 100f;
                scaler.dynamicPixelsPerUnit = 100f;
            }
            ConfigureText(groupText, 72f);
            ConfigureText(propertyNameText, 68f);
            ConfigureText(priceText, 58f);
            ConfigureText(rentTitleText, 48f);
            ConfigureText(baseRentText, 38f);
            ConfigureText(rent1HouseText, 36f);
            ConfigureText(rent2HousesText, 36f);
            ConfigureText(rent3HousesText, 36f);
            ConfigureText(rent4HousesText, 36f);
            ConfigureText(hotelRentText, 36f);
            ConfigureText(housePriceText, 34f);
            ConfigureText(hotelPriceText, 34f);
            if (closeButton)
            {
                var image = closeButton.GetComponent<Image>();
                if (image) image.raycastTarget = true;
                ConfigureText(closeButton.GetComponentInChildren<Text>(true), 52f);
            }
        }

        private static void ConfigureText(Text text, float size)
        {
            if (!text) return;
            text.resizeTextForBestFit = false;
            text.fontSize = Mathf.RoundToInt(size);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.transform.localScale = Vector3.one;
        }

        private void AlignBaseToBoardSurface()
        {
            if (!board || !modelRoot) return;
            var anchor = board.GetAnchor(0);
            if (!anchor) return;
            Vector3 up = board.transform.up.normalized;
            var renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            float minimum = 0f;
            foreach (var renderer in renderers)
            {
                if (!renderer || !renderer.enabled || renderer.bounds.size.sqrMagnitude <= 0.000001f) continue;
                Bounds bounds = renderer.bounds;
                float projectedExtent = Vector3.Dot(new Vector3(Mathf.Abs(up.x), Mathf.Abs(up.y), Mathf.Abs(up.z)), bounds.extents);
                float candidate = Vector3.Dot(bounds.center, up) - projectedExtent;
                if (!hasBounds || candidate < minimum) minimum = candidate;
                hasBounds = true;
            }
            if (!hasBounds) return;
            float surface = Vector3.Dot(anchor.position, up);
            float worldClearance = Mathf.Max(minimumWorldClearance,
                boardClearance * board.transform.TransformVector(Vector3.up).magnitude);
            float shift = surface + worldClearance - minimum;
            if (float.IsNaN(shift) || float.IsInfinity(shift)) return;
            if (shift > 0f) transform.position += up * shift;
        }

        private IEnumerator AnimateOpen()
        {
            Vector3 startPosition = restingPosition - board.transform.up * liftDistance;
            float elapsed = 0f;
            transform.localScale = restingLocalScale * 0.85f;
            while (elapsed < showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / showDuration);
                t = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(startPosition, restingPosition, t);
                transform.rotation = Quaternion.Slerp(restingRotation * Quaternion.Euler(0f, -5f, 0f), restingRotation, t);
                transform.localScale = Vector3.Lerp(restingLocalScale * 0.85f, restingLocalScale, t);
                yield return null;
            }
            transform.position = restingPosition;
            transform.rotation = restingRotation;
            transform.localScale = restingLocalScale;
            animationRoutine = null;
            if (!ValidatePresentation(out string diagnostics))
            {
                FailPresentation("validación al terminar la animación: " + diagnostics);
                yield break;
            }
            LastPresentationValid = true;
            LastPresentationDiagnostics = diagnostics;
            Debug.Log($"[PropertyCard3D] Animación terminada. {diagnostics}", this);
        }

        public bool ValidatePresentation(out string diagnostics)
        {
            diagnostics = string.Empty;
            if (!targetCamera || !targetCamera.isActiveAndEnabled)
            {
                diagnostics = "AR Camera ausente o deshabilitada";
                return false;
            }
            if (!gameObject.activeInHierarchy || !modelRoot || !modelRoot.gameObject.activeInHierarchy ||
                !worldCanvas || !worldCanvas.isActiveAndEnabled)
            {
                diagnostics = $"jerarquía inactiva: root={gameObject.activeInHierarchy}, model={(modelRoot && modelRoot.gameObject.activeInHierarchy)}, canvas={(worldCanvas && worldCanvas.isActiveAndEnabled)}";
                return false;
            }
            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            int activeRenderers = 0;
            foreach (Renderer renderer in renderers)
            {
                if (!renderer || !renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy) continue;
                if (renderer.sharedMaterial && renderer.sharedMaterial.shader && renderer.sharedMaterial.shader.isSupported)
                    activeRenderers++;
            }
            if (renderers.Length == 0 || activeRenderers != renderers.Length || !TryGetModelBounds(out Bounds bounds))
            {
                diagnostics = $"renderers no visibles: activos={activeRenderers}/{renderers.Length}";
                return false;
            }

            Vector3 up = board.transform.up.normalized;
            Vector3 horizontalToCamera = Vector3.ProjectOnPlane(targetCamera.transform.position - transform.position, up);
            if (horizontalToCamera.sqrMagnitude < 0.0001f) horizontalToCamera = -board.transform.forward;
            float frontDot = Vector3.Dot(-transform.forward, horizontalToCamera.normalized);
            GetViewportBounds(bounds, out Vector2 viewportMin, out Vector2 viewportMax, out float farthestDepth);
            float viewportHeight = viewportMax.y - viewportMin.y;
            bool intersectsViewport = farthestDepth > targetCamera.nearClipPlane && viewportMax.x > 0f && viewportMin.x < 1f && viewportMax.y > 0f && viewportMin.y < 1f;
            float baseHeight = MinimumAlong(bounds, up);
            Transform anchor = board.GetAnchor(0);
            if (!anchor) { diagnostics = "anchor 0 ausente"; return false; }
            float boardSurface = Vector3.Dot(anchor.position, up);
            bool aboveBoard = baseHeight >= boardSurface + minimumWorldClearance - 0.002f;
            float canvasFront = Vector3.Dot(worldCanvas.transform.position - bounds.center, -transform.forward);
            float distance = Vector3.Distance(targetCamera.transform.position, bounds.center);
            diagnostics = $"propiedad={CurrentData.index}, pos={transform.position:F3}, escalaMundo={transform.lossyScale:F3}, alturaMundo={ProjectedSize(bounds, up):F3}m, viewport=({viewportMin.x:F2},{viewportMin.y:F2})-({viewportMax.x:F2},{viewportMax.y:F2}), distanciaCam={distance:F2}m, frenteDot={frontDot:F3}, baseSobreTablero={(baseHeight - boardSurface):F3}m, canvasFrente={canvasFront:F4}m, renderers={activeRenderers}/{renderers.Length}, IsOpen={IsOpen}";
            if (!intersectsViewport) { diagnostics = "fuera del viewport o detrás de cámara; " + diagnostics; return false; }
            if (viewportHeight < minimumViewportHeight) { diagnostics = $"altura proyectada insuficiente ({viewportHeight:F3}); " + diagnostics; return false; }
            if (frontDot < 0.90f) { diagnostics = "frente no orientado a cámara; " + diagnostics; return false; }
            if (!aboveBoard) { diagnostics = "tarjeta debajo del tablero; " + diagnostics; return false; }
            if (canvasFront < -0.001f) { diagnostics = "Canvas detrás del modelo; " + diagnostics; return false; }
            if (!CanvasFitsCardSurface(out string canvasDiagnostics))
            {
                diagnostics = canvasDiagnostics + "; " + diagnostics;
                return false;
            }
            return true;
        }

        private bool CanvasFitsCardSurface(out string diagnostics)
        {
            diagnostics = string.Empty;
            if (!worldCanvas || !modelRoot) { diagnostics = "Canvas o modelo ausente"; return false; }
            Transform cardBody = modelRoot.Find("TarjetaPropiedad/CardBody") ?? modelRoot.Find("CardBody");
            if (!cardBody) { diagnostics = "CardBody ausente"; return false; }
            Renderer[] bodyRenderers = cardBody.GetComponentsInChildren<Renderer>(true);
            if (bodyRenderers.Length == 0) { diagnostics = "CardBody sin renderer"; return false; }
            bool hasBounds = false;
            Bounds localBounds = default;
            foreach (Renderer renderer in bodyRenderers)
            {
                if (!renderer) continue;
                Bounds worldBounds = renderer.bounds;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 point = transform.InverseTransformPoint(worldBounds.center + Vector3.Scale(worldBounds.extents, new Vector3(x, y, z)));
                            if (!hasBounds) { localBounds = new Bounds(point, Vector3.zero); hasBounds = true; }
                            else localBounds.Encapsulate(point);
                        }
            }
            RectTransform canvasRect = worldCanvas.transform as RectTransform;
            if (!hasBounds || !canvasRect) { diagnostics = "No se pudo medir la superficie del CardBody"; return false; }
            Vector3[] corners = new Vector3[4];
            canvasRect.GetWorldCorners(corners);
            float marginX = localBounds.size.x * 0.08f;
            float marginY = localBounds.size.y * 0.08f;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 local = transform.InverseTransformPoint(corners[i]);
                if (local.x < localBounds.min.x - marginX || local.x > localBounds.max.x + marginX ||
                    local.y < localBounds.min.y - marginY || local.y > localBounds.max.y + marginY)
                {
                    diagnostics = $"Canvas fuera de CardBody: corner={local:F4}, body={localBounds}";
                    return false;
                }
            }
            return true;
        }

        private void FailPresentation(string reason)
        {
            LastPresentationValid = false;
            LastPresentationDiagnostics = reason;
            Debug.LogWarning($"[PropertyCard3D] No se pudo mostrar la tarjeta. Se libera el turno para evitar bloqueo. {reason}", this);
            CloseInternal("fallo de presentación", true);
        }

        private bool TryGetModelBounds(out Bounds bounds)
        {
            bounds = default;
            if (!modelRoot) return false;
            bool hasBounds = false;
            foreach (Renderer renderer in modelRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer || !renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy || renderer.bounds.size.sqrMagnitude <= 0.0000001f) continue;
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return hasBounds;
        }

        private void GetViewportBounds(Bounds bounds, out Vector2 minimum, out Vector2 maximum, out float farthestDepth)
        {
            minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            farthestDepth = float.NegativeInfinity;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                        Vector3 viewport = targetCamera.WorldToViewportPoint(corner);
                        minimum = Vector2.Min(minimum, viewport);
                        maximum = Vector2.Max(maximum, viewport);
                        farthestDepth = Mathf.Max(farthestDepth, viewport.z);
                    }
        }

        private static float ProjectedSize(Bounds bounds, Vector3 axis)
        {
            axis = new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z));
            return 2f * Vector3.Dot(axis, bounds.extents);
        }

        private static float MinimumAlong(Bounds bounds, Vector3 axis)
        {
            Vector3 absoluteAxis = new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z));
            return Vector3.Dot(bounds.center, axis) - Vector3.Dot(absoluteAxis, bounds.extents);
        }

        private string RendererSummary()
        {
            if (!modelRoot) return "0/0";
            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            int active = 0;
            foreach (Renderer renderer in renderers)
                if (renderer && renderer.enabled && !renderer.forceRenderingOff && renderer.gameObject.activeInHierarchy) active++;
            return $"{active}/{renderers.Length}";
        }

        private static string PlayerName(MonopolyPlayer player)
        {
            return player ? player.displayName : "sin jugador";
        }

        public void UpdateTexts(MonopolySpaceData data)
        {
            if (groupText) groupText.text = data.colorGroup == PropertyColorGroup.None ? "PROPIEDAD" : data.colorGroup.ToString().ToUpperInvariant();
            if (propertyNameText) propertyNameText.text = data.displayName.ToUpperInvariant();
            if (priceText) priceText.text = $"PRECIO ${data.purchasePrice}";
            if (rentTitleText) rentTitleText.text = "ALQUILER";
            if (baseRentText) baseRentText.text = $"Base    ${data.baseRent}";
            if (rent1HouseText) rent1HouseText.text = $"1 casa    ${data.rent1House}";
            if (rent2HousesText) rent2HousesText.text = $"2 casas   ${data.rent2Houses}";
            if (rent3HousesText) rent3HousesText.text = $"3 casas   ${data.rent3Houses}";
            if (rent4HousesText) rent4HousesText.text = $"4 casas   ${data.rent4Houses}";
            if (hotelRentText) hotelRentText.text = $"Hotel     ${data.rentHotel}";
            if (housePriceText) housePriceText.text = $"Casa ${data.housePrice}";
            if (hotelPriceText) hotelPriceText.text = $"Hotel ${data.hotelPrice}";
        }

        public void ApplyHeaderColor(PropertyColorGroup group)
        {
            if (!colorHeaderRenderer) return;
            colorHeaderRenderer.material.color = HeaderColor(group);
        }

        public void ApplyHeaderColor(Color color)
        {
            if (!colorHeaderRenderer) return;
            colorHeaderRenderer.material.color = color;
        }

        public static Color HeaderColor(PropertyColorGroup group)
        {
            switch (group)
            {
                case PropertyColorGroup.Brown: return new Color(0.38f, 0.17f, 0.08f);
                case PropertyColorGroup.LightBlue: return new Color(0.28f, 0.68f, 0.86f);
                case PropertyColorGroup.Pink: return new Color(0.85f, 0.30f, 0.54f);
                case PropertyColorGroup.Orange: return new Color(0.92f, 0.43f, 0.12f);
                case PropertyColorGroup.Red: return new Color(0.78f, 0.10f, 0.08f);
                case PropertyColorGroup.Yellow: return new Color(0.94f, 0.72f, 0.10f);
                case PropertyColorGroup.Green: return new Color(0.12f, 0.50f, 0.22f);
                case PropertyColorGroup.DarkBlue: return new Color(0.08f, 0.20f, 0.55f);
                default: return new Color(0.55f, 0.55f, 0.55f);
            }
        }

        private void OnDestroy()
        {
            foreach (var pair in subscribedPlayers)
                if (pair.Key) pair.Key.ArrivedAtSpace -= OnArrivedAtSpace;
            subscribedPlayers.Clear();
        }
    }
}

