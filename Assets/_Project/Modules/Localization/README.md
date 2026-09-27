# Localization

A small string-localization facade for Unity 6+. Game and UI code use stable keys through `LocalizationService`; one project-owned `ILocalizationProvider` connects the facade to Unity Localization, a file, a remote backend, or another localization system.

The module depends only on `Core/Foundation` (`AsyncGate` and `SafeEvent`). It has no dependency on `com.unity.localization`, a DI container, or a particular table format. Copy the `Localization` and `Foundation` folders into another Unity project.

## Quick start

Implement one provider and return final localized text from `TryGet`:

```csharp
public bool TryGet(string key, object[] arguments, out string text)
{
    // Look up, pluralize, fall back, and format with your localization system.
}
```

Create the service only after the provider has selected and loaded its initial locale:

```csharp
ILocalizationProvider provider = new ProjectLocalizationProvider();
await provider.SetLocaleAsync(savedLocale, cancellationToken);

LocalizationService localization = new(provider);

title.text = localization.Get(LocalizationKeys.QuestRaiderTitle);
objective.text = localization.Get(LocalizationKeys.QuestRaiderObjective, current, required);

localization.LocaleChanged += _ => RebuildVisibleText();
await localization.SetLocaleAsync("uk-UA", cancellationToken); // Resolves to "uk" when available.
```

Use generated keys or a project-owned constants class instead of repeating string literals:

```csharp
public static class LocalizationKeys
{
    public const string QuestRaiderTitle = "quest.raider.title";
}
```

## Unity Localization setup

The example provider requires `com.unity.localization`. Finish Unity's own initialization, load the provider's initial table, and only then create the facade:

```csharp
await LocalizationSettings.InitializationOperation.Task;

var provider = new UnityLocalizationProvider(stringTable);
var initialLocale =
    LocalizationSettings.SelectedLocale.Identifier.Code;

await provider.SetLocaleAsync(initialLocale, cancellationToken);

var localization = new LocalizationService(provider);
```

The provider serves exactly one `LocalizedStringTable`. Use one facade per table, a project-owned composite provider, or prefix routing such as `ui.*` and `quests.*` when a single facade must address multiple tables. Keep localized assets and other package-specific features in Unity Localization directly.

`UnityLocalizationProvider` first checks its loaded table. For a missing or empty entry it calls `LocalizationSettings.StringDatabase.GetTableEntry` with `FallbackBehavior.UseProjectSettings`, so Unity's configured locale fallback chain is applied. This fallback lookup is synchronous and can load a fallback table; preload fallback tables when required, and do not use this implementation on WebGL if a lookup could require synchronous Addressables loading.

Mark `inventory.items` as a Unity Smart String, for example `You have {0:plural:one item|{} items}`, then call:

```csharp
string items = localization.Get("inventory.items", count);
```

`StringTableEntry.GetLocalizedString(arguments)` evaluates the Smart String with the locale of the resolved table. Plural selection belongs to the provider, so the facade deliberately has no `GetPlural` method.

## Ownership and shutdown

The service borrows its provider. `LocalizationService.Dispose()` only unsubscribes from `ILocalizationProvider.LocaleChanged`; it does not dispose or otherwise own the provider. Dispose both objects, in this order:

```csharp
localization.Dispose();
provider.Dispose();
```

Never dispose either object while `SetLocaleAsync` is active. First cancel the operation and await its completion, or simply await the locale change, then dispose the service and provider. `UnityLocalizationBootstrap.cs` demonstrates this lifecycle for a `MonoBehaviour` owner.

After disposal, service lookup, locale access, and locale changes throw `ObjectDisposedException`. Repeated `Dispose()` calls are safe.

## Provider contract

`ILocalizationProvider.TryGet` returns the **finished string**, not a raw template. The provider owns formatting, plural and gender rules, fallback, Smart Strings, table loading, and synchronization with external locale changes. It returns `false` only when the key cannot be resolved.

After a successful locale change, the provider raises `LocaleChanged` only when its new strings are ready. The initial table load raises the event as well, with a null `PreviousLocale`, so subscribers learn the starting locale. It must also raise the event when the backend locale changes outside `LocalizationService`; the service forwards that event to consumers.

Missing keys return `[key]` by default and publish `IssueDetected`. Pass a `Func<string, string>` to the constructor to select another visible fallback. A `FormatException` raised by a provider is reported as `InvalidFormat`; other provider failures remain visible to the caller.

The service uses Foundation's `SafeEvent` when forwarding `LocaleChanged` and publishing `IssueDetected`. Every subscriber is invoked and subscriber exceptions are logged. In the Editor and Development Builds those exceptions are then combined into an `AggregateException`; when a locale notification happens inside a provider call, that aggregate can escape from `SetLocaleAsync`. Production non-development builds log subscriber exceptions without rethrowing them.

Locale requests accept a specific locale or its available parent (`uk-UA` can resolve to `uk`), and concurrent locale changes are serialized. Available locale identifiers must be logically unique: do not expose both `en-US` and `en_US`, or spell the same locale with different casing. `LocaleResolver` treats separators and casing as equivalent, and the service rejects an ambiguous provider during construction.

Create and use the service on Unity's main thread.

## API

```csharp
string Get(string key);
string Get(string key, params object[] arguments);
bool TryGet(string key, out string text);
bool TryGet(string key, out string text, params object[] arguments);
Awaitable SetLocaleAsync(string locale, CancellationToken cancellationToken = default);
```

## Examples and tests

`Assets/UnityTemplates/Examples/Application/Localization` contains removable provider examples:

- `UnityLocalizationProvider.cs` integrates one Unity string table, Smart Strings, project-configured fallback, and external locale changes.
- `UnityLocalizationBootstrap.cs` is an attachable lifecycle example; assign its `LocalizedStringTable` in the Inspector.
- `CsvLocalizationProvider.cs` is a small file-backed provider and intentionally not a production CSV parser.
- `LocalizationExampleLifetimeScope.cs` registers a provider and `LocalizationService` with VContainer.
- `LocalizationExampleScenario.cs` demonstrates typed lookups and an async locale change through an `IAsyncStartable`.
- `LocalizationKeys.cs` shows typed key constants.

Edit Mode tests live in the repository-level `Assets/UnityTemplates/Tests` folder, which mirrors the `Modules` and `Examples` layout (`Tests/Modules/Application/Localization`, `Tests/Examples/Application/Localization`), so a copied module never carries tests with it. The example has no authored scene; add `UnityLocalizationBootstrap` to any scene to run the complete Unity integration lifecycle.
