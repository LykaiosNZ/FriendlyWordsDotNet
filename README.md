# FriendlyWordsDotNet
[![Continuous](https://github.com/LykaiosNZ/FriendlyWordsDotNet/actions/workflows/continuous.yml/badge.svg)](https://github.com/LykaiosNZ/FriendlyWordsDotNet/actions/workflows/continuous.yml)

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