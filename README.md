# FriendlyWordsDotNet
![GitHub Workflow Status](https://img.shields.io/github/workflow/status/LykaiosNZ/FriendlyWordsDotNet/Continuous) ![Nuget](https://img.shields.io/nuget/v/FriendlyWordsDotNet)

A .NET port of the [friendly-words](https://github.com/glitchdotcom/friendly-words) npm package.

## Installing
Install with NuGet:
```
$ dotnet add package FriendlyWordsDotNet
```

## Building
To build using the friendly-words npm package as the source of words:
```sh
# Download the friendly-words words lists (requires npm)
$ ./tools/get-words.sh
# Build the project
$ dotnet build ./src
```

## Credits
* [friendly-words](https://github.com/glitchdotcom/friendly-words) - Provides the words.
* [NUnit](https://github.com/nunit) - Powers the unit tests.
* [Fluent Assertions](https://github.com/fluentassertions/fluentassertions) - Helps out with the unit tests.