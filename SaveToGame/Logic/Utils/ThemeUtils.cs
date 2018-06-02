using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using SaveToGameWpf.Logic.OrganisationItems;

namespace SaveToGameWpf.Logic.Utils
{
    public static class ThemeUtils
    {
        public static void SetThemeFromSettings()
        {
            var settings = DefaultSettingsContainer.Instance;

            if (string.IsNullOrEmpty(settings.Theme))
            {
                settings.Theme = "Light";
                settings.Save();
            }

            SetTheme(settings.Theme);
        }

        public static void SetTheme(string theme)
        {
            var sourceRegex = new Regex(@"Styles/(?<theme>[^/]+)/ThemeResources.xaml");

            var mergedDicts = Application.Current.Resources.MergedDictionaries;

            var current = mergedDicts.WithIndex().WhereNotNull(it => it.value.Source).First(it => sourceRegex.IsMatch(it.value.Source.OriginalString));

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
                    DefaultSettingsContainer.Instance.Theme = "Light";
                    break;
                case "dark":
                    InsertTheme("Dark");
                    DefaultSettingsContainer.Instance.Theme = "Dark";
                    break;
                default:
                    throw new Exception($"Unknown theme: \"{theme}\"");
            }

            mergedDicts.RemoveAt(current.index + 1);
            DefaultSettingsContainer.Instance.Save();
        }
    }
}

