# Voice Falloff Override
### A VRChat mod for MelonLoader by Adnezz, modified by Xan // Eti

Allows you to personally tweak how far away people have to be before you can hear their voice.

Note that unlike Adnezz's copy of the mod, this **does not respect EMM risky function world blacklists.** This fork was designed specifically for individuals that are subject to sensory overload but don't want to mute everyone. Limiting the mod's use directly contradicts its purpose and as such, all limits have been removed.

## Use Cases
- Sometimes voices travel too far and you can't hear your friends over people that are across the room.
- Sometimes the voices of other players are too loud, and it gives you PTSD of the school lunch room.
- Sometimes you are using a large avatar and can't hear people on the ground.

## Requirements
* MelonLoader v0.5.4
* UIExpansionKit is recommended.

## Configuration

If you have UIExpansionKit, all settings are available to change in the mod config menu. The rolloff minimum distance and maximum distances are configurable, as well as a constant gain factor.

After running VRChat with the mod installed for the first time, you can also edit the falloff distance in VRChat/UserData/MelonPreferences.cfg under the VFO header. You **must** do this if you do not have UIExpansionKit, hence why it is recommended.

## Credit
- Origin: dave-kun's [RankVolumeControl](https://github.com/dave-kun/RankVolumeControl)
- Code: NetworkManagerHooks.cs from Knah's [JoinNotifier](https://github.com/knah/VRCMods/tree/master/JoinNotifier)
- Special thanks: lil-fluff (for assistance in tracking down the cause of VFO failing to work in some worlds), Adnezz for original code.

## WARNING: MODS CAN RESULT IN PERMANENT TERMINATION
All modification of the VRChat client is against the VRChat terms of service, which clearly state that it is a bannable offense. While this mod makes no changes apparent to the server, it is possible for VRC to implement checks against mods changing game code.

**Use at your own risk.** I am not responsible if you get banned.