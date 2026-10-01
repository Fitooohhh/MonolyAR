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

        // These normals are expressed in the DieVisual_* parent space, where
        // rotations are applied. The imported prefab contains an internal
        // child rotated about 270 degrees on X, so its mesh-space mapping is
        // not the same as the visual parent's mapping.
        //
        // Visual calibration in Unity confirmed these faces for both current
        // FBX meshes (Dado_01_Mesh and Dado_02_Mesh):
        // 1=+Y, 2=+Z, 3=-X, 4=+X, 5=-Z, 6=-Y.
        public static Vector3 FaceNormalFor(int face)
        {
            return FaceNormalFor(1, face);
        }

        public static Vector3 FaceNormalFor(int dieIndex, int face)
        {
            if (dieIndex != 1 && dieIndex != 2)
                throw new System.ArgumentOutOfRangeException(nameof(dieIndex), "El dado debe ser 1 o 2.");

            // Keep one branch per die so a future FBX with a different
            // orientation can be calibrated without changing the API.
            switch (dieIndex)
            {
                case 1:
                    switch (face)
                    {
                        case 1: return Vector3.up;
                        case 2: return Vector3.forward;
                        case 3: return Vector3.left;
                        case 4: return Vector3.right;
                        case 5: return Vector3.back;
                        case 6: return Vector3.down;
                        default: throw new System.ArgumentOutOfRangeException(nameof(face), "La cara debe estar entre 1 y 6.");
                    }
                case 2:
                    switch (face)
                    {
                        case 1: return Vector3.up;
                        case 2: return Vector3.forward;
                        case 3: return Vector3.left;
                        case 4: return Vector3.right;
                        case 5: return Vector3.back;
                        case 6: return Vector3.down;
                        default: throw new System.ArgumentOutOfRangeException(nameof(face), "La cara debe estar entre 1 y 6.");
                    }
                default: throw new System.ArgumentOutOfRangeException(nameof(dieIndex));
            }
        }

        public static Quaternion FaceUpRotationFor(int face, Quaternion currentRotation)
        {
            return FaceUpRotationFor(1, face, currentRotation);
        }

        public static Quaternion FaceUpRotationFor(int dieIndex, int face, Quaternion currentRotation)
        {
            return Quaternion.FromToRotation(currentRotation * FaceNormalFor(dieIndex, face), Vector3.up) * currentRotation;
        }

        // Diagnostic calibration path: directly places a physical face up,
        // without random values, animation, movement, or turn changes.
        public void SetCalibrationFace(int dieIndex, int face)
        {
            Transform die = dieIndex == 1 ? dieOneVisual : dieIndex == 2 ? dieTwoVisual : null;
            if (!die) throw new System.InvalidOperationException("Falta el modelo del dado para calibración.");
            die.localRotation = FaceUpRotationFor(dieIndex, face, Quaternion.identity);
        }

        public void SetCalibrationFaces(int faceOne, int faceTwo)
        {
            SetCalibrationFace(1, faceOne);
            SetCalibrationFace(2, faceTwo);
        }

        private static float EaseOutCubic(float t)
        {
            float inverse = 1f - Mathf.Clamp01(t);
            return 1f - inverse * inverse * inverse;
        }

        private static void AnimateDie(Transform die, Vector3 basePosition, Quaternion baseRotation,
            Quaternion finalRotation, float t, float height, float lateral, Vector3 spin)
        {
            float normalized = Mathf.Clamp01(t);
            float arc = Mathf.Sin(normalized * Mathf.PI) * height;
            float bounce = 0f;
            if (normalized > .70f)
            {
                float bounceT = (normalized - .70f) / .30f;
                bounce = Mathf.Sin(bounceT * Mathf.PI * 2f) * .045f * (1f - bounceT);
            }
            die.localPosition = basePosition
                + Vector3.up * (arc + bounce)
                + Vector3.right * (Mathf.Sin(normalized * Mathf.PI) * lateral);
            die.localRotation = Quaternion.Slerp(baseRotation, finalRotation, EaseOutCubic(normalized))
                * Quaternion.Euler(spin * normalized);
        }

        public IEnumerator Roll()
        {
            if (IsRolling || !dieOneVisual || !dieTwoVisual) throw new System.InvalidOperationException("Dados no disponibles.");
            IsRolling = true;
            ClearResult();
            Vector2Int result = GenerateRoll();
            Vector3 aPosition = dieOneVisual.localPosition;
            Vector3 bPosition = dieTwoVisual.localPosition;
            Quaternion aRotation = dieOneVisual.localRotation;
            Quaternion bRotation = dieTwoVisual.localRotation;
            Quaternion aFinalRotation = FaceUpRotationFor(1, result.x, aRotation);
            Quaternion bFinalRotation = FaceUpRotationFor(2, result.y, bRotation);
            float duration = Mathf.Clamp(rollSeconds, 1.2f, 2f);
            bool completed = false;
            try
            {
                for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
                {
                    float t = elapsed / duration;
                    AnimateDie(dieOneVisual, aPosition, aRotation, aFinalRotation, t, .35f, .13f, new Vector3(720f, 1080f, 1440f));
                    AnimateDie(dieTwoVisual, bPosition, bRotation, bFinalRotation, t, .42f, -.11f, new Vector3(-1080f, 1440f, 720f));
                    yield return null;
                }
                dieOneVisual.localPosition = aPosition;
                dieTwoVisual.localPosition = bPosition;
                dieOneVisual.localRotation = aFinalRotation;
                dieTwoVisual.localRotation = bFinalRotation;
                DieOne = result.x;
                DieTwo = result.y;
                completed = true;
            }
            finally
            {
                if (!completed)
                {
                    dieOneVisual.localPosition = aPosition;
                    dieTwoVisual.localPosition = bPosition;
                    dieOneVisual.localRotation = aRotation;
                    dieTwoVisual.localRotation = bRotation;
                    ClearResult();
                }
                IsRolling = false;
            }
        }
    }
}
