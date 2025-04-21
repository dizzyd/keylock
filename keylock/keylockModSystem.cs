using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace keylock;

public class keylockModSystem : ModSystem
{
    private ICoreClientAPI capi;
    private Dictionary<GlKeys, bool> lockedKeys = new Dictionary<GlKeys, bool>();
    private GlKeys toggleKey = GlKeys.F8;

    public override bool ShouldLoad(EnumAppSide forSide)
    {
        // Only load this mod on the client side
        return forSide == EnumAppSide.Client;
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        this.capi = api;

        // Register a hotkey for toggling key lock state
        api.Input.RegisterHotKey("keylockertoggle", Lang.Get("keylock_descriptor"), toggleKey, 
            HotkeyType.GUIOrOtherControls);
        api.Input.SetHotKeyHandler("keylockertoggle", OnToggleKeyLock);

        // Register for game tick to simulate locked keys being pressed
        api.Event.RegisterGameTickListener(OnGameTick, 20);
    }

    private bool OnToggleKeyLock(KeyCombination comb)
    {   
        // Walk through any locked keys and depress them; otherwise, the key will never
        // get unlocked because the walk through pressed keys will already show them as pressed
        foreach (var key in lockedKeys.Keys)
        {
            capi.Logger.Audit($"Depressing key: {key}");
            capi.Input.KeyboardKeyState[(int)key] = false;
        }
        lockedKeys.Clear();
        
        // Find any keys currently being pressed; they need to be locked
        for (int i = 0; i < capi.Input.KeyboardKeyStateRaw.Length; i++)
        {
            if (capi.Input.KeyboardKeyStateRaw[i] && i != (int)toggleKey)
            {
                GlKeys key = (GlKeys)i;
                lockedKeys[key] = true;
                capi.ShowChatMessage($"{Lang.Get("keylock_pressed")}: {GlKeyNames.ToString(key)}");
            }
        }
        
        return true;
    }

    private void OnGameTick(float dt)
    {
        // Early exit if no keys are locked
        if (lockedKeys.Count == 0) return;

        // Simulate key presses for all locked keys
        foreach (var key in lockedKeys.Keys)
        {
            // The KeyboardKeyState is what's used for movement/actions,
            // so we need to make sure it shows our locked keys as pressed
            capi.Input.KeyboardKeyState[(int)key] = true;
        }
    }

    public override void Dispose()
    {
        // Clear locked keys when mod is unloaded
        lockedKeys.Clear();
    }
}