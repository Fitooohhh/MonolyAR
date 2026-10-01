using System.Collections;
using System.Linq;
using UnityEngine;

namespace MonopolyAR
{
    public enum TurnPhase { NotReady, AwaitRoll, Rolling, Moving, AwaitEnd, Error }

    public sealed class MonopolyTurnManager : MonoBehaviour
    {
        public MonopolyBoard board;
        public MonopolyPlayer[] players;
        [Tooltip("Colección completa de jugadores disponibles. El orden determina el orden de los turnos.")]
        public MonopolyPlayer[] playerRoster;
        [Range(2, 4)] public int activePlayerCount = 2;
        public MonopolyDice dice;
        [Min(0.02f)] public float secondsPerTile = 0.24f;
        public TurnPhase Phase { get; private set; } = TurnPhase.NotReady;
        public int ActivePlayerIndex { get; private set; }
        public MonopolyPlayer ActivePlayer => players[ActivePlayerIndex];
        public int ActivePlayerCount => players == null ? 0 : players.Length;
        public MonopolyPlayer[] ConfiguredPlayers => playerRoster != null && playerRoster.Length > 0 ? playerRoster : players;
        public string ErrorMessage { get; private set; }
        public bool CanRoll => Phase == TurnPhase.AwaitRoll;
        public bool CanEndTurn => Phase == TurnPhase.AwaitEnd;

        private void Start()
        {
            try
            {
                InitializePlayers();
                Phase = TurnPhase.AwaitRoll;
            }
            catch (System.Exception ex)
            {
                ErrorMessage = ex.Message;
                Phase = TurnPhase.Error;
                Debug.LogException(ex, this);
            }
        }

        /// <summary>
        /// Configura la partida antes de colocar el tablero. El menú usa este único
        /// punto de entrada; al habilitarse el componente, Start vuelve a validar
        /// y preparar la misma selección antes del primer turno.
        /// </summary>
        public bool ConfigurePlayers(int count)
        {
            if (count < 2 || count > 4) return false;
            activePlayerCount = count;
            try
            {
                InitializePlayers();
                Phase = enabled ? TurnPhase.AwaitRoll : TurnPhase.NotReady;
                ErrorMessage = null;
                return true;
            }
            catch (System.Exception ex)
            {
                ErrorMessage = ex.Message;
                Phase = TurnPhase.Error;
                Debug.LogException(ex, this);
                return false;
            }
        }

        private void InitializePlayers()
        {
            var roster = ConfiguredPlayers;
            if (!board || !dice || roster == null || roster.Length < activePlayerCount || activePlayerCount < 2 || activePlayerCount > 4)
                throw new System.InvalidOperationException("Configura entre dos y cuatro jugadores, tablero y dados.");
            board.Validate();
            foreach (var player in roster)
            {
                if (!player) throw new System.InvalidOperationException("Falta un jugador.");
                player.Validate();
                player.ResetForNewGame();
                player.PlaceAt(board, 0);
                player.gameObject.SetActive(false);
            }
            players = roster.Take(activePlayerCount).ToArray();
            foreach (var player in players) player.gameObject.SetActive(true);
            if (!dice.dieOneVisual || !dice.dieTwoVisual) throw new System.InvalidOperationException("Faltan los modelos de dados.");
            ActivePlayerIndex = 0;
            dice.ClearResult();
        }

        public bool TryRoll()
        {
            if (!CanRoll) return false;
            Phase = TurnPhase.Rolling;
            StartCoroutine(RollAndMove());
            return true;
        }

        private IEnumerator RollAndMove()
        {
            yield return dice.Roll();
            Phase = TurnPhase.Moving;
            yield return ActivePlayer.MoveSteps(board, dice.Sum, secondsPerTile);
            Phase = TurnPhase.AwaitEnd;
        }

        public bool TryEndTurn()
        {
            if (!CanEndTurn) return false;
            ActivePlayerIndex = (ActivePlayerIndex + 1) % players.Length;
            dice.ClearResult();
            Phase = TurnPhase.AwaitRoll;
            return true;
        }
    }
}
