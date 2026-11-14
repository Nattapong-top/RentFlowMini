namespace RentFlowApp.Application.Settings;

public interface IAppearanceSettingsRepository
{
    AppearanceMode Load();

    void Save(AppearanceMode mode);
}
