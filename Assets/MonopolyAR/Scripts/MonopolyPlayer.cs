using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MonopolyAR
{
    public sealed class MonopolyPlayer : MonoBehaviour
    {
        public string displayName;
        public Vector2 separation;
        public int CurrentTile { get; private set; }
        public bool IsMoving { get; private set; }
        private readonly List<int> visited = new List<int>();
        public IReadOnlyList<int> LastVisited => visited;

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
                }
            }
            finally
            {
                IsMoving = false;
            }
        }
    }
}
