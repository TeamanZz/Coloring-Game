using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Network.Utils
{
    public class Lang
    {
        public static string DeviceLang()
        {
            var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            return allCultures.First(x => x.DisplayName.ToLower() == Application.systemLanguage.ToString().ToLower()).Name;
        }
    }
}
