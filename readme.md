
# GTAV RewardHook
GTAV RewardHook is a plugin for ScriptHookDotNet that turns GTA V into a reinforcement learning environment.

The idea is to accumulate facts relevant to safe driving (such as average road alignment, collisions etc.) in each training episode. These stats can then be translated to a single reward using some configurable reward scheme, and can be stitched together with screenshots taken around the same time to create datasets for reinforcement learning agents.

## Disclaimer
The publisher of GTA V allows non-commercial use of footage from the game. Please do not use any datasets generated using RewardHook for any commercial projects. This work is also not affiliated with any organization that I am a part of and is just a personal project :)

## Getting Started

### Requirements
1. GTA V (with ScriptHook and ScriptHookDotNet installed)
2. ScriptHookDotNet (for compilation, from nuget)
3. Nancy (for RESTful API, from nuget)

### Installation
1. Download / clone the repository.
2. Open the project, and build using Visual Studio (preferably using VS2017 or later).
    - Visual studio should also be able to pick up nuget dependencies when you import.
3. Drag the compiled dll to `scripts` in your GTAV root.
4. Start the game. You should now see occasional notifications.

## Pending Tasks
RewardHook is still a work in progress. While a lot of events can be extracted using the known list of native methods (from GTA's RAGE engine), there are still several events that we cannot reliably detect (like when we running red lights, or driving offroad, or even lane alignment), unless the corresponding native addresses are known.

For some of these events, alternative tracking mechanisms do exist. For example, it is possible to detect if an NPC is waiting at a red light, which can help us create datasets that penalize running red lights _only_ when another NPC is actually waiting and has been travelling with us.

Fortunately, the list of natives is constantly growing as the modding community discovers more natives (I use [this](www.dev-c.com/nativedb) list as a reference).

Besides these limitations, any pending tasks are usually in [TODO.md](todo.md).

## Contributing
Contributions are welcome. Feel free to send a pull request.