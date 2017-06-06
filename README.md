# Cashew
Cashew is a .NET library for caching responses easily with an HttpClient through an API that is simple and elegant yet powerful.
There's support out of the box for the awesome [CacheManager](https://github.com/MichaCo/CacheManager) via the `Cashew.Adapters.CacheManager` package.

Cashew is a .NET library for caching HTTP responses on  through an API that is simple and elegant yet powerful. 
It's built on top of the awesome [CacheManager](https://github.com/MichaCo/CacheManager) with a focus on extensibility and being easy to use.

Cashew targets .NET 4.5 and .NET Standard 1.1 (.NET Core, Mono, Xamarin.iOS, Xamarin.Android, UWP and [more](https://github.com/dotnet/standard/blob/master/docs/versions.md)) meaning it can be used on all sorts of devices.

# Installation
The latest versions of the packages are available on NuGet. To install, run the following command if you want to roll your own cache:
```
PM> Install-Package Cashew
```
or the command below if you want to utilise the power of [CacheManager](https://github.com/MichaCo/CacheManager)
```
PM> Install-Package Cashew.Adapters.CacheManager
```

# Features




## Cache stores

|Type|Quickstart|In-depth|Out of the box?|
| ------------- | ------------- | ------------- |------------- |
|Dictionary|  |  |Yes*|
|[System.Runtime.Caching.MemoryCache](https://msdn.microsoft.com/en-us/library/system.runtime.caching.memorycache(v=vs.110).aspx) |  | |Yes*|
|[Microsoft.Extensions.Caching.Memory](https://github.com/aspnet/Caching/tree/dev/src/Microsoft.Extensions.Caching.Memory)|||Yes*|
| [Redis](https://www.nuget.org/packages/CacheManager.StackExchange.Redis) |  |  |Yes*|
| [Memcached](https://www.nuget.org/packages/CacheManager.Memcached) |  |  |Yes*|
| [Couchbase](https://www.nuget.org/packages/CacheManager.Couchbase) |  |  |Yes*|
| Custom | | |No| 

*Provided that you use `Cashew.Adapters.CacheManager`

## Customisation
Cashew provides a lot of customisation opportunities for its users. The most important ones are listed below:

|Feature|Quickstart|In-depth|
| ------------- | ------------- | ------------- |
| Use any cache store | [Link]() | [Wiki](https://github.com/joakimskoog/Cashew/wiki) |
| Decide how cache keys are created | [Link](#decide-how-cache-keys-are-created) | [Wiki](https://github.com/joakimskoog/Cashew/wiki) |
| Decide which status codes are cacheable | [Link](#cacheable-status-codes) | [Wiki](https://github.com/joakimskoog/Cashew/wiki) |

# Usage

## Configuring HttpClient


## Use any cache store
```csharp
//We feel like caching the HTTP responses in an SQL store (for some reason) and have therefore created our own SqlCache
var sqlCache = new SqlCache();

//We pass our newly created sql cache in the constructor and watch the magic happen
var httpCachingHandler = new HttpCachingHandler(sqlCache, keyStrategy);
```

## Decide how cache keys are created
```csharp
//We have created our own strategy that creates keys out of request URI:s
var uriKeyStrategy = new RequestUriKeyStrategy();

//We pass our newly created key strategy in the constructor and watch the magic happen!
var httpCachingHandler = new HttpCachingHandler(memoryCache, uriKeyStrategy);

```

## Cacheable status codes
```csharp
//We only want to cache responses with status 200
httpCachingHandler.CacheableStatusCodes = new[] { HttpStatusCode.OK };
```



# Contributing
