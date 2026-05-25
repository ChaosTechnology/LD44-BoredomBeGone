# LD48 - Boredom Be Gone

This game was programmed during [Ludum Dare 44].  

What is Ludum Dare?  
Short version: You have 72 hours. Build a game!

Check here for the [original submission]!

We have used C# as a programming language, our own [ChaosFramework], OpenGL 3.3 for graphics and OpenAL for audio.

# The Concept
**TODO: copy from the LD submission page once that's up again**

# Controls
**TODO: copy from the LD submission page once that's up again**

# Building and running

## Requirements
- [dotnet 8.0 SDK] or higher on Windows

## Building
1. Clone the repository, including its submodules:  
  ```
  git clone --recurse-submodules https://github.com/ChaosTechnology/LD44-BoredomBeGone.git
  ```
2. Run `run.sh` in a bash of your choice.

# Trouble Shooting
The game will generate a settings.confix file in the build output directory that allows you to customize some settings. If you're having performance issues you can lower the solid world resolution as well as the transparency resolution. Transparency usually has a way larger impact than the solid world. You can also lower the number of correctly sorted layers of transparency, which has an even greater effect on performance.

[Ludum Dare 44]: TODO
[original submission]: TODO
[dotnet 8.0 SDK]: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
[ChaosFramework]: https://github.com/ChaosFramework