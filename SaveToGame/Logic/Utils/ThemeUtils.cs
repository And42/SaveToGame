using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Interfaces.OrganisationItems;

namespace SaveToGameWpf.Logic.Utils
{
    public class ThemeUtils
    {
        private readonly IAppSettings _appSettings;
        
        public ThemeUtils(
            IAppSettings appSettings
        )
        {
            _appSettings = appSettings;
        }
        
        public void SetThemeFromSettings()
        {
            string settingsTheme = _appSettings.Theme;
            if (string.IsNullOrEmpty(settingsTheme))
                _appSettings.Theme = settingsTheme = "Light";

            SetTheme(settingsTheme);
        }

        public static void SetTheme(string theme)
        {
            var sourceRegex = new Regex(@"Styles/(?<theme>[^/]+)/ThemeResources.xaml");

            var mergedDicts = Application.Current.Resources.MergedDictionaries;

            var current = mergedDicts.Enumerate().WhereNotNull(it => it.value.Source).First(it => sourceRegex.IsMatch(it.value.Source.OriginalString));

            string currentTheme = sourceRegex.Match(current.value.Source.OriginalString).Groups["theme"].Value;

            if (currentTheme.Equals(theme, StringComparison.OrdinalIgnoreCase))
                return;

            void InsertTheme(string resTheme)
            {
                mergedDicts.Insert(
                    current.index, 
                    new ResourceDictionary { Source = new Uri($"Styles/{resTheme}/ThemeResources.xaml", UriKind.Relative) }
                );
            }

            switch (theme.ToLower())
            {
                case "light":
                    InsertTheme("Light");
                    break;
                case "dark":
                    InsertTheme("Dark");
                    break;
                default:
                    throw new Exception($"Unknown theme: \"{theme}\"");
            }

            mergedDicts.RemoveAt(current.index + 1);
        }
    }
}

