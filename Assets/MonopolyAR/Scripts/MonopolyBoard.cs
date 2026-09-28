using System;
using System.Linq;
using UnityEngine;

namespace MonopolyAR
{
    public sealed class MonopolyBoard : MonoBehaviour
    {
        public const int TileCount = 40;
        [SerializeField] private Transform[] anchors = new Transform[TileCount];
        [SerializeField] private Transform[] tiles = new Transform[TileCount];
        public Transform[] Anchors => (Transform[])anchors.Clone();

        // Index is defined by the original names, never by hierarchy order or coordinates.
        public void RegisterOriginalAnchors()
        {
            var children = GetComponentsInChildren<Transform>(true);
            anchors = new Transform[TileCount];
            tiles = new Transform[TileCount];
            for (int i = 0; i < TileCount; i++)
            {
                string name = $"Tile_{i:00}";
                anchors[i] = children.Single(t => t.name == name + "_Anchor");
                tiles[i] = children.Single(t => t.name == name);
            }
            Validate();
        }

        public void Validate()
        {
            if (anchors.Length != TileCount || tiles.Length != TileCount)
                throw new InvalidOperationException("Se requieren las 40 casillas y anclajes originales.");
            for (int i = 0; i < TileCount; i++)
                if (!anchors[i] || !tiles[i] || !anchors[i].IsChildOf(transform) ||
                    !tiles[i].IsChildOf(transform) || anchors[i].name != $"Tile_{i:00}_Anchor" || tiles[i].name != $"Tile_{i:00}")
                    throw new InvalidOperationException($"Referencia original inválida en casilla {i}.");
        }

        public Transform GetAnchor(int index)
        {
            if (index < 0 || index >= TileCount) throw new ArgumentOutOfRangeException(nameof(index));
            return anchors[index];
        }

        public Vector3 PositionFor(int index, Vector2 separation)
        {
            return GetAnchor(index).position + transform.TransformVector(new Vector3(separation.x, 0, separation.y));
        }

        public static int NextIndex(int index)
        {
            if (index < 0 || index >= TileCount) throw new ArgumentOutOfRangeException(nameof(index));
            return (index + 1) % TileCount;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (anchors == null) return;
            for (int i = 0; i < anchors.Length; i++)
                if (anchors[i]) UnityEditor.Handles.Label(anchors[i].position + transform.up * 0.12f, i == 0 ? "00 SALIDA" : $"{i:00}");
        }
#endif
    }
}
