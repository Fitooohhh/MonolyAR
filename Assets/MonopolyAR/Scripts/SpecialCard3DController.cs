using UnityEngine;

namespace MonopolyAR
{
    /// <summary>
    /// Handles special-space rules while reusing the same 3D card presentation
    /// used by properties. This proxy owns no second mesh, Canvas, or text system.
    /// </summary>
    public sealed class SpecialCard3DController : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public bool DecisionPending { get; private set; }
        public MonopolySpaceData CurrentData { get; private set; }
        public MonopolyPlayer CurrentPlayer { get; private set; }
        public string Summary { get; private set; }
        public SpaceType CurrentType => CurrentData.type;
        public PropertyCard3DController VisualCard => propertyCard;
        public bool JailOptionsVisible => IsOpen && CurrentData.type == SpaceType.Jail && CurrentPlayer && CurrentPlayer.IsInJail;

        private MonopolyBoard board;
        private PropertyCard3DController propertyCard;
        private bool effectApplied;

        private void Awake()
        {
            board = FindObjectOfType<MonopolyBoard>(true);
            propertyCard = FindObjectOfType<PropertyCard3DController>(true);
            IsOpen = false;
            DecisionPending = false;
        }

        private void EnsureReferences()
        {
            if (!board) board = FindObjectOfType<MonopolyBoard>(true);
            if (!propertyCard) propertyCard = FindObjectOfType<PropertyCard3DController>(true);
        }

        public void Show(MonopolyPlayer player, MonopolySpaceData data)
        {
            EnsureReferences();
            CurrentPlayer = player;
            CurrentData = data;
            effectApplied = false;
            ApplyEffect();

            bool shown = propertyCard && propertyCard.ShowSpecial(
                player,
                data,
                TitleFor(data.type),
                SymbolFor(data.type),
                Summary,
                HeaderColor(data.type));

            IsOpen = shown && propertyCard.IsOpen;
            DecisionPending = IsOpen;
            if (!IsOpen)
                Debug.LogWarning("[SpecialCard3D] No se pudo abrir la tarjeta especial reutilizable; el turno queda desbloqueado.", this);
        }

        private void ApplyEffect()
        {
            if (effectApplied || !CurrentPlayer || !board) return;
            effectApplied = true;
            if (CurrentData.type == SpaceType.Tax)
            {
                int amount = CurrentData.index == 4 ? 200 : 100;
                int paid = Mathf.Min(CurrentPlayer.Money, amount);
                if (paid > 0) CurrentPlayer.TrySpend(paid);
                if (paid < amount) board.AddDebt(CurrentPlayer, amount - paid);
                Summary = $"{CurrentData.displayName}\nDebes ${amount}\nPagado ${paid}\nDeuda pendiente ${amount - paid}";
            }
            else if (CurrentData.type == SpaceType.GoToJail)
            {
                CurrentPlayer.SendToJail(board);
                Summary = "Ve directamente a la cárcel.\nNo cobras por pasar por SALIDA.";
            }
            else if (CurrentData.type == SpaceType.Jail)
                Summary = CurrentPlayer.IsInJail ? "Estás en la cárcel.\nPuedes pagar $50 o intentar dobles." : "Solo visita.\nNo ocurre ningún efecto.";
            else if (CurrentData.type == SpaceType.FreeParking)
                Summary = "Estacionamiento gratis.\nNo se cobra ni se entrega dinero.";
            else if (CurrentData.type == SpaceType.Chance)
            {
                int amount = CurrentData.index % 2 == 0 ? 50 : -50;
                if (amount > 0) CurrentPlayer.AddMoney(amount); else CurrentPlayer.TrySpend(-amount);
                Summary = amount > 0 ? $"Recibes ${amount}." : $"Pagas ${-amount}.";
            }
            else if (CurrentData.type == SpaceType.Community)
            {
                CurrentPlayer.AddMoney(50);
                Summary = "Caja de Comunidad\nRecibes $50.";
            }
            else Summary = CurrentData.displayName;
        }

        public void Acknowledge()
        {
            if (DecisionPending) Close();
        }

        public bool PayJailFine()
        {
            if (!JailOptionsVisible || !CurrentPlayer.TrySpend(50)) return false;
            CurrentPlayer.ReleaseFromJail();
            Summary = "Pagaste $50 y saliste de la cárcel.";
            Close();
            return true;
        }

        public bool AttemptJailRoll()
        {
            if (!JailOptionsVisible) return false;
            var dice = FindObjectOfType<MonopolyDice>(true);
            if (!dice || !dice.HasResult) return false;
            if (dice.DieOne == dice.DieTwo)
            {
                CurrentPlayer.ReleaseFromJail();
                Summary = "Sacaste dobles y saliste de la cárcel.";
                Close();
                return true;
            }
            CurrentPlayer.RegisterJailAttempt();
            Summary = $"No sacaste dobles. Intentos restantes: {Mathf.Max(0, 3 - CurrentPlayer.JailAttempts)}";
            if (propertyCard) propertyCard.UpdateSpecialSummary(Summary);
            return false;
        }

        public bool UseJailCard()
        {
            if (!JailOptionsVisible) return false;
            Summary = "No tienes una tarjeta para salir de la cárcel.";
            if (propertyCard) propertyCard.UpdateSpecialSummary(Summary);
            return false;
        }

        public void Close()
        {
            if (propertyCard) propertyCard.Close();
            IsOpen = false;
            DecisionPending = false;
            effectApplied = false;
            CurrentPlayer = null;
        }

        private static string TitleFor(SpaceType type)
        {
            switch (type)
            {
                case SpaceType.Chance: return "SUERTE";
                case SpaceType.Community: return "CAJA DE COMUNIDAD";
                case SpaceType.Tax: return "IMPUESTOS";
                case SpaceType.Jail:
                case SpaceType.GoToJail: return "CÁRCEL";
                case SpaceType.FreeParking: return "ESTACIONAMIENTO GRATIS";
                default: return "CASILLA ESPECIAL";
            }
        }

        private static string SymbolFor(SpaceType type)
        {
            switch (type)
            {
                case SpaceType.Chance: return "?";
                case SpaceType.Community: return "C";
                case SpaceType.Tax: return "$";
                case SpaceType.Jail:
                case SpaceType.GoToJail: return "J";
                case SpaceType.FreeParking: return "P";
                default: return "!";
            }
        }

        private static Color HeaderColor(SpaceType type)
        {
            switch (type)
            {
                case SpaceType.Chance: return new Color(.95f, .48f, .08f);
                case SpaceType.Community: return new Color(.25f, .52f, .90f);
                case SpaceType.Tax: return new Color(.78f, .12f, .10f);
                case SpaceType.Jail:
                case SpaceType.GoToJail: return new Color(.15f, .15f, .17f);
                case SpaceType.FreeParking: return new Color(.18f, .62f, .28f);
                default: return Color.white;
            }
        }
    }
}
