using System.Collections;
using UnityEngine;

namespace MonopolyAR
{
    public sealed class MonopolyDice : MonoBehaviour
    {
        public Transform dieOneVisual;
        public Transform dieTwoVisual;
        [Min(0.1f)] public float rollSeconds = 0.7f;
        public int DieOne { get; private set; }
        public int DieTwo { get; private set; }
        public int Sum => DieOne + DieTwo;
        public bool HasResult => DieOne > 0 && DieTwo > 0;
        public bool IsRolling { get; private set; }

        public static Vector2Int GenerateRoll() => new Vector2Int(Random.Range(1, 7), Random.Range(1, 7));
        public void ClearResult() { DieOne = 0; DieTwo = 0; }

        // Cosmetic only: numeric results are authoritative, not the visible top faces.
        public IEnumerator Roll()
        {
            if (IsRolling || !dieOneVisual || !dieTwoVisual) throw new System.InvalidOperationException("Dados no disponibles.");
            IsRolling = true;
            ClearResult();
            var a = dieOneVisual.localRotation;
            var b = dieTwoVisual.localRotation;
            try
            {
                for (float elapsed = 0; elapsed < rollSeconds; elapsed += Time.deltaTime)
                {
                    dieOneVisual.Rotate(new Vector3(420, 300, 200) * Time.deltaTime, Space.Self);
                    dieTwoVisual.Rotate(new Vector3(-300, 400, 250) * Time.deltaTime, Space.Self);
                    yield return null;
                }
                var result = GenerateRoll();
                DieOne = result.x;
                DieTwo = result.y;
            }
            finally
            {
                dieOneVisual.localRotation = a;
                dieTwoVisual.localRotation = b;
                IsRolling = false;
            }
        }
    }
}
