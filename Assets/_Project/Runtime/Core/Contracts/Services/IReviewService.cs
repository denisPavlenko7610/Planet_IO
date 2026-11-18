namespace PlanetIO
{
    public interface IReviewService
    {
        bool IsAvailable { get; }

        void Request();
    }
}
