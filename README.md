# Cashew
Cashew is a .NET library for caching responses easily with an HttpClient through an API that is simple and elegant yet powerful.
There's support out of the box for the awesome [CacheManager](https://github.com/MichaCo/CacheManager) via the `Cashew.Adapters.CacheManager` package. Its aim is to focus on the HTTP part of caching and not worrying about how stuff is stored, meaning no half-arsed cache implementations!

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
- Extremely easy to use, all it takes is one line to configure the whole thing!
- Simple but powerful API that allows customisation
- [ETag support](https://en.wikipedia.org/wiki/HTTP_ETag)
- [Vary Header support](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Vary)

## Cache stores

|Type|Quickstart|In-depth|Out of the box?|
| ------------- | ------------- | ------------- |------------- |
|Dictionary|  |  |Yes*|
|[System.Runtime.Caching.MemoryCache](https://msdn.microsoft.com/en-us/library/system.runtime.caching.memorycache(v=vs.110).aspx) |  | |Yes*|
|[Microsoft.Extensions.Caching.Memory](https://github.com/aspnet/Caching/tree/dev/src/Microsoft.Extensions.Caching.Memory)|||Yes*|
| [Redis](https://www.nuget.org/packages/CacheManager.StackExchange.Redis) |  |  |Yes*|
| [Memcached](https://www.nuget.org/packages/CacheManager.Memcached) |  |  |Yes*|
| [Couchbase](https://www.nuget.org/packages/CacheManager.Couchbase) |  |  |Yes*|
| Custom | | |No, but it's super easy to implement your own.| 

*Provided that you use `Cashew.Adapters.CacheManager`

## HTTP Cache-Control Headers
|Header|Aka|
| ------------- | ------------- |
|[max-age](https://tools.ietf.org/html/rfc7234)|"I don't want cached responses older than this"|
|[s-maxage](https://tools.ietf.org/html/rfc7234)|"I don't want cached responses older than this"| 
|[max-stale](https://tools.ietf.org/html/rfc7234)|"Stale responses are OK for this long"| 
|[min-fresh](https://tools.ietf.org/html/rfc7234)|"The response has to still be fresh for at least this long"| 
|[no-cache](https://tools.ietf.org/html/rfc7234)|"You must validate the cached response with the server| 
|[no-store](https://tools.ietf.org/html/rfc7234)|"DO NOT CACHE THIS OR I WILL MAKE YOUR LIFE MISERABLE!"| 
|[only-if-cached](https://tools.ietf.org/html/rfc7234)|"I only want a response if it's cached"| 
|[must-revalidate](https://tools.ietf.org/html/rfc7234)|"You MUST revalidate stale responses"| 
|[proxy-revalidate](https://tools.ietf.org/html/rfc7234)|"You MUST revalidate stale responses"| 

## Customisation
Cashew provides a lot of customisation opportunities for its users. The most important ones are listed below:

|Feature|Quickstart|In-depth|
| ------------- | ------------- | ------------- |
| Use any cache store | [Link](#use-any-cache-store) | [Wiki](https://github.com/joakimskoog/Cashew/wiki/Custom-cache) |
| Decide how cache keys are created | [Link](#decide-how-cache-keys-are-created) | [Wiki](https://github.com/joakimskoog/Cashew/wiki/CacheKeyStrategy) |
| Decide which status codes are cacheable | [Link](#cacheable-status-codes) | [Wiki](https://github.com/joakimskoog/Cashew/wiki/HTTP-Status-Codes) |

# Usage
For more in-depth information on how to use Cashew, please refer to our [wiki](https://github.com/joakimskoog/Cashew/wiki).

## Configuring HttpClient
```csharp
//All it takes is one line to configure the whole thing!
var httpClient = new HttpClient(new HttpCachingHandler(cache, new HttpStandardKeyStrategy(cache)));
```


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

## Decide how query strings are handled
```csharp
//The default implementation of ICacheKeyStrategy is HttpStandardKeyStrategy. You can configure it to handle query strings in two ways.

//Using CacheKeySetting.Standard will result in a different cache key each time the query string changes
var queryStringStrategy = new HttpStandardKeyStrategy(cache, CacheKeySetting.Standard);

//Using CacheKeySetting.IgnoreQueryString will result in the same key even if the query string changes.
var uriStrategy = new HttpStandardKeyStrategy(cache, CacheKeySetting.IgnoreQueryString);
```

## Cacheable status codes
```csharp
//We only want to cache responses with status 200
httpCachingHandler.CacheableStatusCodes = new[] { HttpStatusCode.OK };
```

# Contributing
Please refer to our [guidelines](https://github.com/joakimskoog/Cashew/wiki/Contributing) on contributing.
