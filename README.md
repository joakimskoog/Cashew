# Cashew
Cashew is a .NET library for caching responses easily with an HttpClient through an API that is simple and elegant yet powerful.
There's support out of the box for the awesome [CacheManager](https://github.com/MichaCo/CacheManager) via the `Cashew.Adapters.CacheManager` package.

Cashew is a .NET library for caching HTTP responses on  through an API that is simple and elegant yet powerful. 
It's built on top of the awesome [CacheManager](https://github.com/MichaCo/CacheManager) with a focus on extensibility and being easy to use.

Cashew targets .NET 4.5 and .NET Standard 1.1 (.NET Core, Mono, Xamarin.iOS, Xamarin.Android, UWP and [more](https://github.com/dotnet/standard/blob/master/docs/versions.md)) meaning it can be used on all sorts of devices.

## Installation
The latest versions of the packages are available on NuGet. To install, run the following command if you want to roll your own cache:
```
PM> Install-Package Cashew.Core
```
or the command below if you want to utilise the power of [CacheManager](https://github.com/MichaCo/CacheManager)
```
PM> Install-Package Cashew.Adapters.CacheManager
```

## Features

### Cache stores

|Type|Out of the box?|More Info|
| ------------- | ------------- | ------------- |
| Dictionary | Yes* |  |
| [System.Runtime.Caching.MemoryCache](https://msdn.microsoft.com/en-us/library/system.runtime.caching.memorycache(v=vs.110).aspx) | Yes* | |
| [Microsoft.Extensions.Caching.Memory](https://github.com/aspnet/Caching/tree/dev/src/Microsoft.Extensions.Caching.Memory) | Yes* |  |
| [Redis](https://www.nuget.org/packages/CacheManager.StackExchange.Redis) | Yes |  |
| [Memcached](https://www.nuget.org/packages/CacheManager.Memcached) | Yes* |  |
| [Couchbase](https://www.nuget.org/packages/CacheManager.Couchbase) | Yes* |  |
| Custom | No | [Information on how to roll your own cache store](https://github.com/joakimskoog/Cashew) |

*Provided that you use the `CacheManager` package.

### Customisation
Cashew provides a lot of customisation opportunities for its users. The most important ones are listed below:

|Customisation feature|More info|
| ------------- | ------------- |
| Use any cache technology | [Wiki](https://github.com/joakimskoog/Cashew/wiki) |
| Decide how cache keys are created | [Wiki](https://github.com/joakimskoog/Cashew/wiki) |
| Decide which status codes are cacheable | [Wiki](https://github.com/joakimskoog/Cashew/wiki) |

### More information
If you're interested in quick code examples for all Cashew features, refer to the section below. If you instead want a more in-depth explanation on each and every feature, please refer to the [Wiki](https://github.com/joakimskoog/Cashew/wiki)


## Code examples


## Contributing
