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


## General features
- No need to keep tabs on it, configure once and start running
- Used as a DelegatingHandler inside the HttpClient meaning it's easy to use
- Simple but powerful API
- [ETag](https://en.wikipedia.org/wiki/HTTP_ETag)
- [Vary Header](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Vary)

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

## HTTP Cache-Control Headers
|Header|Aka|Quickstart|In-depth|
| ------------- | ------------- | ------------- | ------------- |
|[max-age](https://tools.ietf.org/html/rfc7234)|"I dont want cached responses older than this"| | |
|[s-maxage](https://tools.ietf.org/html/rfc7234)|"I dont want cached responses older than this"| | |
|[max-stale](https://tools.ietf.org/html/rfc7234)|"Stale responses are OK for this long"| | |
|[min-fresh](https://tools.ietf.org/html/rfc7234)|"The response has to still be fresh for at least this long"| | |
|[no-cache](https://tools.ietf.org/html/rfc7234)|"You must validate the cached response with the server| | |
|[no-store](https://tools.ietf.org/html/rfc7234)|"DO NOT CACHE THIS OR I WILL MAKE YOUR LIFE MISERABLE!"| | |
|[only-if-cached](https://tools.ietf.org/html/rfc7234)|"I only want a response if it's cached"| | |
|[must-revalidate](https://tools.ietf.org/html/rfc7234)|"You MUST revalidate stale responses"| | |
|[proxy-revalidate](https://tools.ietf.org/html/rfc7234)|"You MUST revalidate stale responses"| | |

## Customisation
Cashew provides a lot of customisation opportunities for its users. The most important ones are listed below:

|Feature|Quickstart|In-depth|
| ------------- | ------------- | ------------- |
| Use any cache store | [Link](#use-any-cache-store) | [Wiki](https://github.com/joakimskoog/Cashew/wiki) |
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
