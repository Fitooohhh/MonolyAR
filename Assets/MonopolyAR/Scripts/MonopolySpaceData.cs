using System;

namespace MonopolyAR
{
    public enum SpaceType
    {
        Start,
        Property,
        Community,
        Tax,
        Station,
        Chance,
        Jail,
        Utility,
        FreeParking,
        GoToJail
    }

    public enum PropertyColorGroup
    {
        None,
        Brown,
        LightBlue,
        Pink,
        Orange,
        Red,
        Yellow,
        Green,
        DarkBlue
    }

    [Serializable]
    public struct MonopolySpaceData
    {
        public int index;
        public string displayName;
        public SpaceType type;
        public PropertyColorGroup colorGroup;
        public int purchasePrice;
        public int baseRent;
        public int rent1House;
        public int rent2Houses;
        public int rent3Houses;
        public int rent4Houses;
        public int rentHotel;
        public int housePrice;
        public int hotelPrice;

        public MonopolySpaceData(int index, string displayName, SpaceType type, PropertyColorGroup colorGroup)
        {
            this.index = index;
            this.displayName = displayName;
            this.type = type;
            this.colorGroup = colorGroup;
            purchasePrice = 0;
            baseRent = 0;
            rent1House = 0;
            rent2Houses = 0;
            rent3Houses = 0;
            rent4Houses = 0;
            rentHotel = 0;
            housePrice = 0;
            hotelPrice = 0;
        }
    }

    public static class MonopolySpaceDataCatalog
    {
        public static MonopolySpaceData[] CreateDefault()
        {
            var spaces = new MonopolySpaceData[MonopolyBoard.TileCount];
            for (int i = 0; i < spaces.Length; i++)
                spaces[i] = new MonopolySpaceData(i, "Casilla " + i.ToString("00"), SpaceType.Property, PropertyColorGroup.None);

            Set(spaces, 0, "SALIDA", SpaceType.Start);
            Set(spaces, 1, "Propiedad 01", SpaceType.Property, PropertyColorGroup.Brown);
            Set(spaces, 2, "Comunidad", SpaceType.Community);
            Set(spaces, 3, "Propiedad 03", SpaceType.Property, PropertyColorGroup.Brown);
            Set(spaces, 4, "Impuesto", SpaceType.Tax);
            Set(spaces, 5, "Estación 1", SpaceType.Station);
            Set(spaces, 6, "Propiedad 06", SpaceType.Property, PropertyColorGroup.LightBlue);
            Set(spaces, 7, "Suerte", SpaceType.Chance);
            Set(spaces, 8, "Propiedad 08", SpaceType.Property, PropertyColorGroup.LightBlue);
            Set(spaces, 9, "Propiedad 09", SpaceType.Property, PropertyColorGroup.LightBlue);
            Set(spaces, 10, "Cárcel / Visita", SpaceType.Jail);
            Set(spaces, 11, "Propiedad 11", SpaceType.Property, PropertyColorGroup.Pink);
            Set(spaces, 12, "Electricidad", SpaceType.Utility);
            Set(spaces, 13, "Propiedad 13", SpaceType.Property, PropertyColorGroup.Pink);
            Set(spaces, 14, "Propiedad 14", SpaceType.Property, PropertyColorGroup.Pink);
            Set(spaces, 15, "Estación 2", SpaceType.Station);
            Set(spaces, 16, "Propiedad 16", SpaceType.Property, PropertyColorGroup.Orange);
            Set(spaces, 17, "Comunidad", SpaceType.Community);
            Set(spaces, 18, "Propiedad 18", SpaceType.Property, PropertyColorGroup.Orange);
            Set(spaces, 19, "Propiedad 19", SpaceType.Property, PropertyColorGroup.Orange);
            Set(spaces, 20, "Parking", SpaceType.FreeParking);
            Set(spaces, 21, "Propiedad 21", SpaceType.Property, PropertyColorGroup.Red);
            Set(spaces, 22, "Suerte", SpaceType.Chance);
            Set(spaces, 23, "Propiedad 23", SpaceType.Property, PropertyColorGroup.Red);
            Set(spaces, 24, "Propiedad 24", SpaceType.Property, PropertyColorGroup.Red);
            Set(spaces, 25, "Estación 3", SpaceType.Station);
            Set(spaces, 26, "Propiedad 26", SpaceType.Property, PropertyColorGroup.Yellow);
            Set(spaces, 27, "Propiedad 27", SpaceType.Property, PropertyColorGroup.Yellow);
            Set(spaces, 28, "Agua", SpaceType.Utility);
            Set(spaces, 29, "Propiedad 29", SpaceType.Property, PropertyColorGroup.Yellow);
            Set(spaces, 30, "Ir a la Cárcel", SpaceType.GoToJail);
            Set(spaces, 31, "Propiedad 31", SpaceType.Property, PropertyColorGroup.Green);
            Set(spaces, 32, "Propiedad 32", SpaceType.Property, PropertyColorGroup.Green);
            Set(spaces, 33, "Comunidad", SpaceType.Community);
            Set(spaces, 34, "Propiedad 34", SpaceType.Property, PropertyColorGroup.Green);
            Set(spaces, 35, "Estación 4", SpaceType.Station);
            Set(spaces, 36, "Suerte", SpaceType.Chance);
            Set(spaces, 37, "Propiedad 37", SpaceType.Property, PropertyColorGroup.DarkBlue);
            Set(spaces, 38, "Impuesto", SpaceType.Tax);
            Set(spaces, 39, "Propiedad 39", SpaceType.Property, PropertyColorGroup.DarkBlue);
            return spaces;
        }

        private static void Set(MonopolySpaceData[] spaces, int index, string name, SpaceType type,
            PropertyColorGroup color = PropertyColorGroup.None)
        {
            spaces[index] = new MonopolySpaceData(index, name, type, color);
        }
    }
}
