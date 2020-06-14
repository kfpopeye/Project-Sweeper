using WPFLocalizeExtension.Extensions;

namespace PKHL.ProjectSweeper
{
    public static class LocalizationProvider
    {
        public static T GetLocalizedValue<T>(string key)
        {
            return LocExtension.GetLocalizedValue<T>("Project Sweeper:Language:" + key);
        }
    }
}

