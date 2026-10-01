using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace MonopolyAR
{
    public sealed class MonopolyPlayer : MonoBehaviour
    {
        public const int StartingMoney = 1500;
        public const int StartBonus = 200;
        public string displayName;
        public Vector2 separation;
        [SerializeField] private int money = StartingMoney;
        public int CurrentTile { get; private set; }
        public bool IsMoving { get; private set; }
        public int Money => money;
        public event Action<MonopolyPlayer> PassedStart;
        private readonly List<int> visited = new List<int>();
        public IReadOnlyList<int> LastVisited => visited;

        public void ResetForNewGame()
        {
            if (IsMoving) throw new System.InvalidOperationException("No se puede reiniciar el dinero durante el movimiento.");
            money = StartingMoney;
        }

        public void PlaceAt(MonopolyBoard board, int index)
        {
            if (IsMoving) throw new System.InvalidOperationException("El jugador ya se está moviendo.");
            CurrentTile = index;
            transform.position = board.PositionFor(index, separation);
        }

        public void Validate()
        {
            // El modelo estatico necesita una malla visible, no clips ni Animator.
            if (!GetComponentInChildren<MeshRenderer>(true))
                throw new System.InvalidOperationException("Falta el modelo del jugador: " + displayName);
        }

        public IEnumerator MoveSteps(MonopolyBoard board, int steps, float secondsPerTile)
        {
            if (steps < 1 || secondsPerTile <= 0 || IsMoving) throw new System.ArgumentOutOfRangeException(nameof(steps));
            IsMoving = true;
            visited.Clear();
            try
            {
                for (int step = 0; step < steps; step++)
                {
                    int previous = CurrentTile;
                    int next = MonopolyBoard.NextIndex(CurrentTile);
                    Vector3 start = transform.position;
                    Vector3 target = board.PositionFor(next, separation);
                    Vector3 direction = Vector3.ProjectOnPlane(target - start, board.transform.up);
                    if (direction.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direction, board.transform.up);
                    float elapsed = 0;
                    while (elapsed < secondsPerTile)
                    {
                        elapsed += Time.deltaTime;
                        transform.position = Vector3.Lerp(start, board.PositionFor(next, separation), Mathf.Clamp01(elapsed / secondsPerTile));
                        yield return null;
                    }
                    transform.position = board.PositionFor(next, separation);
                    CurrentTile = next;
                    visited.Add(next);
                    if (previous == MonopolyBoard.TileCount - 1 && next == 0)
                    {
                        money += StartBonus;
                        PassedStart?.Invoke(this);
                    }
                }
            }
            finally
            {
                IsMoving = false;
            }
        }
    }
}
