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
        [SerializeField] private int lapsCompleted;
        [SerializeField] private Animator animator;
        [SerializeField, Range(0f, 25f)] private float walkSwingDegrees = 12f;
        public int CurrentTile { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsWalkingAnimationActive { get; private set; }
        public int Money => money;
        public int LapsCompleted => lapsCompleted;
        public bool IsInJail { get; private set; }
        public int JailAttempts { get; private set; }
        public event Action<MonopolyPlayer> PassedStart;
        public event Action<MonopolyPlayer, MonopolySpaceData> ArrivedAtSpace;
        public bool TrySpend(int amount)
        {
            if (amount < 0 || money < amount) return false;
            money -= amount;
            return true;
        }

        public void AddMoney(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            money += amount;
        }
        private readonly List<int> visited = new List<int>();
        private readonly List<Transform> walkingLimbs = new List<Transform>();
        private readonly List<Quaternion> walkingLimbRotations = new List<Quaternion>();
        public IReadOnlyList<int> LastVisited => visited;

        private void Awake()
        {
            if (!animator) animator = GetComponent<Animator>();
            CacheWalkingLimbs();
        }

        private void CacheWalkingLimbs()
        {
            walkingLimbs.Clear(); walkingLimbRotations.Clear();
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child == transform) continue;
                string n = child.name.ToLowerInvariant();
                if (!n.Contains("manga") && !n.Contains("brazo") && !n.Contains("pierna") && !n.Contains("pantalon")) continue;
                walkingLimbs.Add(child); walkingLimbRotations.Add(child.localRotation);
            }
        }

        private void Update()
        {
            if (!IsWalkingAnimationActive) return;
            float wave = Mathf.Sin(Time.time * 8f) * walkSwingDegrees;
            for (int i = 0; i < walkingLimbs.Count; i++)
            {
                Transform limb = walkingLimbs[i];
                if (!limb) continue;
                bool right = limb.name.EndsWith("_R", StringComparison.OrdinalIgnoreCase) || limb.name.EndsWith("_r", StringComparison.OrdinalIgnoreCase);
                float sign = right ? -1f : 1f;
                limb.localRotation = walkingLimbRotations[i] * Quaternion.Euler(wave * sign, 0f, 0f);
            }
        }

        private void SetWalkingAnimation(bool active)
        {
            IsWalkingAnimationActive = active;
            if (animator && animator.runtimeAnimatorController)
            {
                animator.applyRootMotion = false;
                animator.SetBool("Walking", active);
            }
            if (!active)
                for (int i = 0; i < walkingLimbs.Count; i++)
                    if (walkingLimbs[i]) walkingLimbs[i].localRotation = walkingLimbRotations[i];
        }

        public void ResetForNewGame()
        {
            if (IsMoving) throw new System.InvalidOperationException("No se puede reiniciar el dinero durante el movimiento.");
            money = StartingMoney;
            lapsCompleted = 0;
            visited.Clear();
            IsInJail = false;
            JailAttempts = 0;
        }

        public void SendToJail(MonopolyBoard board)
        {
            if (!board) return;
            SetWalkingAnimation(false);
            IsInJail = true;
            JailAttempts = 0;
            CurrentTile = 10;
            transform.position = board.PositionFor(10, separation);
        }

        public void ReleaseFromJail()
        {
            IsInJail = false;
            JailAttempts = 0;
        }

        public void RegisterJailAttempt() { if (IsInJail) JailAttempts++; }

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
            SetWalkingAnimation(true);
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
                        lapsCompleted++;
                        money += StartBonus;
                        PassedStart?.Invoke(this);
                    }
                }
                ArrivedAtSpace?.Invoke(this, board.GetSpaceData(CurrentTile));
            }
            finally
            {
                SetWalkingAnimation(false);
                IsMoving = false;
            }
        }
    }
}
