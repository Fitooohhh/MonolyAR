using System.Collections;
using UnityEngine;

namespace MonopolyAR
{
    public enum TurnPhase { NotReady, AwaitRoll, Rolling, Moving, AwaitEnd, Error }

    public sealed class MonopolyTurnManager : MonoBehaviour
    {
        public MonopolyBoard board;
        public MonopolyPlayer[] players;
        public MonopolyDice dice;
        [Min(0.02f)] public float secondsPerTile = 0.24f;
        public TurnPhase Phase { get; private set; } = TurnPhase.NotReady;
        public int ActivePlayerIndex { get; private set; }
        public MonopolyPlayer ActivePlayer => players[ActivePlayerIndex];
        public string ErrorMessage { get; private set; }
        public bool CanRoll => Phase == TurnPhase.AwaitRoll;
        public bool CanEndTurn => Phase == TurnPhase.AwaitEnd;

        private void Start()
        {
            try
            {
                if (!board || !dice || players == null || players.Length < 2 || players.Length > 4)
                    throw new System.InvalidOperationException("Configura entre dos y cuatro jugadores, tablero y dados.");
                board.Validate();
                foreach (var player in players)
                {
                    if (!player) throw new System.InvalidOperationException("Falta un jugador.");
                    player.Validate();
                    player.ResetForNewGame();
                    player.PlaceAt(board, 0);
                }
                if (!dice.dieOneVisual || !dice.dieTwoVisual) throw new System.InvalidOperationException("Faltan los modelos de dados.");
                ActivePlayerIndex = 0;
                dice.ClearResult();
                Phase = TurnPhase.AwaitRoll;
            }
            catch (System.Exception ex)
            {
                ErrorMessage = ex.Message;
                Phase = TurnPhase.Error;
                Debug.LogException(ex, this);
            }
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
