namespace Planet_IO
{
    public interface IRoomPreferences
    {
        RoomConnectionSettings Load();
        void Save(RoomConnectionSettings settings);
    }
}
