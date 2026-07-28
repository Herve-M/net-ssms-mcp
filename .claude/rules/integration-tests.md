---
description: "Integration test guidelines"
paths: ["**/tests/*.Integration/**/*.cs"]
---

# Integration Test Guidelines

## Rules

Default behavior:

- do not introduce other testing helper libraries
- do not modify csproj
- Follow Arrange-Act-Assert (AAA).
- Name tests as `MethodName_Scenario_ExpectedBehavior`.

Path and namespace mirroring:

- A test MUST mirror the source folder structure of the type it covers, one-to-one.
- General mapping rule: for a source type at `src/<Project>/<folder...>/<MyClass>.cs`, the test
  lives at `tests/<Project>.Integration/<folder...>/<MyClass>Tests.cs` — preserve every
  subfolder after the project root and append `Tests` to the class filename.
    - Example: `src/Server.Mcp/Cli/MainConfigurationFactory.cs`
      → `tests/Server.Mcp.Integration/Cli/MainConfigurationFactoryTests.cs`.
    - Example: `src/Infrastructure/Persistence/<X>.cs`
      → `tests/Infrastructure.Integration/Persistence/<X>Tests.cs`.
- Namespace MUST match the resulting test folder hierarchy
  (e.g. `ssmsmcp.Server.Mcp.Integration.Cli`).

All cases minimum coverage:

- Cover all public methods on the target type unless user explicitly scopes down.
- Include at least one happy path and one negative or error path for each behavioral area.
- Include empty or not-found behavior where applicable.

Execution under Microsoft.Testing.Platform:

- Prefer focused execution via `runTests` using explicit `testNames` and or target files.
- If `dotnet test --filter` is ignored by MTP, report it and switch to name-based focused execution.

Exception for deterministic time:

- If the class under test depends on `TimeProvider`, prefer `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing`.

## References

- [unit-testing-csharp-with-xunit](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-csharp-with-xunit)
- [microsoft-testing-platform](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
- [Organizing and testing projects with the .NET CLI](https://learn.microsoft.com/en-us/dotnet/core/tutorials/testing-with-cli)
- [Microsoft.Testing.Platform overview](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro)
- [Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0&pivots=xunit)
- [xUnit.v3 samples](https://github.com/xunit/samples.xunit/tree/main/v3)
- [WireMock samples](https://github.com/wiremock/WireMock.Net/blob/master/examples/)
- [Testcontainers](https://github.com/testcontainers/testcontainers-dotnet)
