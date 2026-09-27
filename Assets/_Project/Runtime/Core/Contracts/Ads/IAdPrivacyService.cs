using System;

namespace PlanetIO
{
    public interface IAdPrivacyService
    {
        event Action PrivacyOptionsChanged;

        bool IsPrivacyOptionsRequired { get; }

        void ShowPrivacyOptions();
    }
}
