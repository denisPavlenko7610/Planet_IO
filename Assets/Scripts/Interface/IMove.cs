namespace Planet_IO
{
    interface IMove
    {
        float NormalSpeed { get; set; }
        float BoostSpeed { get; set; }
        void Move();
    }
}
