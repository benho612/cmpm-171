using UnityEngine;
using UnityEngine.InputSystem;

public static class RebindSaveLoad
{
    private const string PlayerPrefsKey = "input_rebinds";

    public static void SaveAllBindings(InputActionAsset asset)
    {
        var json = asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(PlayerPrefsKey, json);
        PlayerPrefs.Save();
    }

    public static void LoadAllBindings(InputActionAsset asset)
    {
        if (!PlayerPrefs.HasKey(PlayerPrefsKey)) return;

        var json = PlayerPrefs.GetString(PlayerPrefsKey);
        asset.LoadBindingOverridesFromJson(json);
    }

    public static void ClearAllBindings(InputActionAsset asset)
    {
        asset.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(PlayerPrefsKey);
        PlayerPrefs.Save();
    }
}