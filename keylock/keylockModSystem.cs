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
        api.Logger.Audit("Starting keylock client side");

        // Register a hotkey for toggling key lock state with improved registration
        api.Input.RegisterHotKey("keylockertoggle", Lang.Get("keylock:key_description"), toggleKey, 
            HotkeyType.CharacterControls);
        api.Input.SetHotKeyHandler("keylockertoggle", OnToggleKeyLock);
        
        // Register for game tick to simulate locked keys being pressed
        api.Event.RegisterGameTickListener(OnGameTick, 20);
        
        // Show startup message to confirm mod is loaded (use the actual registered key)
        var actualKey = api.Input.GetHotKeyByCode("keylockertoggle");

        api.ShowChatMessage(Lang.Get("keylock:loaded_msg", actualKey.CurrentMapping));
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
        var hadLockedKeys = lockedKeys.Count > 0;
        lockedKeys.Clear();
        
        // Find any keys currently being pressed; they need to be locked
        var keyString = "";
        for (int i = 0; i < capi.Input.KeyboardKeyStateRaw.Length; i++)
        {
            if (capi.Input.KeyboardKeyStateRaw[i] && i != (int)toggleKey)
            {
                GlKeys key = (GlKeys)i;
                lockedKeys[key] = true;
                keyString += " " + GlKeyNames.ToString(key);
            }
        }

        if (keyString != "")
        {
            capi.ShowChatMessage(Lang.Get("keylock:locked_msg", keyString));
        }
        else if (hadLockedKeys)
        {
            capi.ShowChatMessage(Lang.Get("keylock:unlocked_msg"));
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
            capi.Input.KeyboardKeyState[(int)key] = true;
        }
    }

    public override void Dispose()
    {
        // Clear locked keys when mod is unloaded
        lockedKeys.Clear();
    }
}