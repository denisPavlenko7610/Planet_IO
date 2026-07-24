namespace PlanetIO
{
    public interface IRoomPreferences
    {
        RoomConnectionSettings Load();
        void Save(RoomConnectionSettings settings);
    }
}
