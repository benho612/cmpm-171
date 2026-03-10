using UnityEngine;
using System.Collections;
using UnityEngine.Localization.Settings;

public class LocaleSelector : MonoBehaviour
{
    private bool active = false;
    public void changeLocale(int localeID){
        if(active) return;
        StartCoroutine(SetLocale(localeID));
    }

    IEnumerator SetLocale(int localeID){
        active = true;
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeID];
        active = false;
    }

}
