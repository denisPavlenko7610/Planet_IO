namespace Planet_IO
{
    public interface IMove
    {
        float NormalSpeed { get; set; }
        float BoostSpeed { get; set; }
        void Move();
    }
}
