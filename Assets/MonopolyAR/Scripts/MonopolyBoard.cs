using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace MonopolyAR
{
    public sealed class MonopolyBoard : MonoBehaviour
    {
        public const int TileCount = 40;
        [SerializeField] private Transform[] anchors = new Transform[TileCount];
        [SerializeField] private Transform[] tiles = new Transform[TileCount];
        [SerializeField] private MonopolySpaceData[] spaces = new MonopolySpaceData[TileCount];
        private readonly Dictionary<int, MonopolyPlayer> propertyOwners = new Dictionary<int, MonopolyPlayer>();
        private readonly Dictionary<MonopolyPlayer, int> outstandingDebts = new Dictionary<MonopolyPlayer, int>();
        private readonly HashSet<string> rentSettledThisTurn = new HashSet<string>();
        public Transform[] Anchors => (Transform[])anchors.Clone();
        public MonopolySpaceData[] Spaces => (MonopolySpaceData[])spaces.Clone();
        public MonopolyPlayer GetOwner(int index) => propertyOwners.TryGetValue(index, out var owner) ? owner : null;
        public void ClearPropertyOwners() { propertyOwners.Clear(); outstandingDebts.Clear(); rentSettledThisTurn.Clear(); }
        public void ClearRentResolutions() => rentSettledThisTurn.Clear();
        public int GetOutstandingDebt(MonopolyPlayer player) => player && outstandingDebts.TryGetValue(player, out var debt) ? debt : 0;
        public void AddDebt(MonopolyPlayer player, int amount)
        {
            if (!player || amount <= 0) return;
            outstandingDebts[player] = GetOutstandingDebt(player) + amount;
        }

        public bool TryCollectRent(MonopolyPlayer visitor, int index, int diceTotal, out MonopolyPlayer owner,
            out int rent, out int paid, out int debt, out string reason)
        {
            owner = GetOwner(index); rent = 0; paid = 0; debt = 0; reason = string.Empty;
            MonopolySpaceData data = GetSpaceData(index);
            if (!visitor || owner == null || owner == visitor) return false;
            string settlementKey = visitor.GetInstanceID() + ":" + index;
            if (rentSettledThisTurn.Contains(settlementKey)) { reason = "Alquiler ya liquidado para esta llegada."; return false; }
            if (data.type == SpaceType.Property) rent = data.baseRent;
            else if (data.type == SpaceType.Station)
            {
                int count = 0;
                foreach (var pair in propertyOwners) if (pair.Value == owner && GetSpaceData(pair.Key).type == SpaceType.Station) count++;
                rent = count == 1 ? 25 : count == 2 ? 50 : count == 3 ? 100 : 200;
            }
            else if (data.type == SpaceType.Utility)
            {
                int count = 0;
                foreach (var pair in propertyOwners) if (pair.Value == owner && GetSpaceData(pair.Key).type == SpaceType.Utility) count++;
                rent = Mathf.Max(0, diceTotal) * (count >= 2 ? 10 : 4);
            }
            else return false;
            paid = Mathf.Min(visitor.Money, rent);
            if (paid > 0) visitor.TrySpend(paid);
            if (paid > 0) owner.AddMoney(paid);
            debt = rent - paid;
            if (debt > 0) outstandingDebts[visitor] = GetOutstandingDebt(visitor) + debt;
            rentSettledThisTurn.Add(settlementKey);
            reason = debt > 0 ? "Fondos insuficientes; deuda registrada." : "Alquiler cobrado.";
            return true;
        }

        public bool TryPurchase(MonopolyPlayer player, int index, out string reason)
        {
            reason = string.Empty;
            if (!player) { reason = "Jugador inválido."; return false; }
            MonopolySpaceData data = GetSpaceData(index);
            if (data.type != SpaceType.Property && data.type != SpaceType.Station && data.type != SpaceType.Utility)
            { reason = "La casilla no es comprable."; return false; }
            if (propertyOwners.ContainsKey(index)) { reason = "La propiedad ya tiene dueño."; return false; }
            if (data.purchasePrice < 0) { reason = "Precio inválido."; return false; }
            if (!player.TrySpend(data.purchasePrice)) { reason = "Dinero insuficiente."; return false; }
            propertyOwners[index] = player;
            return true;
        }

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
            EnsureSpaceData();
            Validate();
        }

        public void Validate()
        {
            EnsureSpaceData();
            if (anchors.Length != TileCount || tiles.Length != TileCount)
                throw new InvalidOperationException("Se requieren las 40 casillas y anclajes originales.");
            for (int i = 0; i < TileCount; i++)
                if (!anchors[i] || !tiles[i] || !anchors[i].IsChildOf(transform) ||
                    !tiles[i].IsChildOf(transform) || anchors[i].name != $"Tile_{i:00}_Anchor" || tiles[i].name != $"Tile_{i:00}")
                    throw new InvalidOperationException($"Referencia original inválida en casilla {i}.");
        }

        public MonopolySpaceData GetSpaceData(int index)
        {
            if (index < 0 || index >= TileCount) throw new ArgumentOutOfRangeException(nameof(index));
            EnsureSpaceData();
            return spaces[index];
        }

        public void EnsureSpaceData()
        {
            if (spaces == null || spaces.Length != TileCount)
            {
                spaces = MonopolySpaceDataCatalog.CreateDefault();
                return;
            }

            for (int i = 0; i < TileCount; i++)
                if (spaces[i].index != i || string.IsNullOrEmpty(spaces[i].displayName))
                {
                    spaces = MonopolySpaceDataCatalog.CreateDefault();
                    return;
                }

            // Keep the authored board names/types while synchronizing the
            // classic purchase prices into scenes saved before prices existed.
            for (int i = 0; i < TileCount; i++)
                spaces[i].purchasePrice = MonopolySpaceDataCatalog.DefaultPurchasePrice(i);
            for (int i = 0; i < TileCount; i++)
                spaces[i].baseRent = MonopolySpaceDataCatalog.DefaultBaseRent(i);
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
