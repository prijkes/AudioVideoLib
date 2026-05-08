namespace AudioVideoLib.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using AudioVideoLib;
using AudioVideoLib.Tags;

using Xunit;

/// <summary>
/// Registry coverage pin for the <c>GetIdentifierFromFactory</c> contract.
/// <para />
/// Every concrete <see cref="Id3v2Frame"/> subclass whose <see cref="Id3v2Frame.Identifier"/>
/// override delegates to the factory must be registered in <c>Id3v2Frame.FrameFactories</c>
/// for every <see cref="Id3v2Version"/> its <c>IsVersionSupported</c> accepts. Reaching
/// the factory with an unregistered <c>(Type, Version)</c> pair now throws
/// <see cref="InvalidOperationException"/> — this test pins that no such pair exists today,
/// so the contract is enforced by tests instead of relying on a string-literal fallback.
/// </summary>
public class Id3v2FrameIdentifierRegistryTests
{
    private static readonly Id3v2Version[] AllVersions =
    [
        Id3v2Version.Id3v220,
        Id3v2Version.Id3v221,
        Id3v2Version.Id3v230,
        Id3v2Version.Id3v240,
    ];

    [Fact]
    public void EveryConstructibleFrameType_HasIdentifierForEverySupportedVersion()
    {
        // Reflect over the AudioVideoLib assembly for non-abstract Id3v2Frame subclasses.
        // For each, attempt construction at each of the four Id3v2Version values:
        //   - Skip the (version, IsVersionSupported = false) cases (ctor throws InvalidVersionException).
        //   - For everything that constructs, the Identifier property must be non-null/non-empty.
        // This walks the entire concrete-frame surface, so a future registration mistake
        // (e.g. adding a new IsVersionSupported branch without updating Id3v2FrameFactory)
        // will fail this test instead of silently falling through to a literal.
        var assembly = typeof(Id3v2Frame).Assembly;
        var concreteFrameTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Id3v2Frame)))
            .ToList();

        // Sanity: the assembly contains a healthy number of concrete frame types.
        Assert.True(concreteFrameTypes.Count >= 30,
            $"Expected at least 30 concrete Id3v2Frame subclasses, found {concreteFrameTypes.Count}.");

        var checkedPairs = 0;
        var failures = new List<string>();

        foreach (var type in concreteFrameTypes)
        {
            // Look for an ctor that takes (Id3v2Version) — this is the canonical entrypoint.
            // Two frames in the registry (Id3v2LinkedInformationFrame, Id3v2TextFrame, Id3v2UrlLinkFrame)
            // also accept an identifier; pick the simplest signature available so the test stays
            // pragmatic.
            var versionOnlyCtor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: [typeof(Id3v2Version)],
                modifiers: null);

            var versionAndIdCtor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: [typeof(Id3v2Version), typeof(string)],
                modifiers: null);

            if (versionOnlyCtor == null && versionAndIdCtor == null)
            {
                // Frames without a version-accepting ctor (e.g. helper/header types) are out of scope.
                continue;
            }

            foreach (var version in AllVersions)
            {
                Id3v2Frame? frame;
                try
                {
                    if (versionOnlyCtor != null)
                    {
                        frame = (Id3v2Frame)versionOnlyCtor.Invoke([version]);
                    }
                    else
                    {
                        // Id3v2TextFrame / Id3v2UrlLinkFrame / Id3v2LinkedInformationFrame need an
                        // identifier. Pick the canonical one for v2.3+ ("TIT2" / "WCOM" / "LINK").
                        var seedIdentifier = type == typeof(Id3v2UrlLinkFrame) ? "WCOM"
                            : type == typeof(Id3v2LinkedInformationFrame) ? "LINK"
                            : "TIT2";
                        frame = (Id3v2Frame)versionAndIdCtor!.Invoke([version, seedIdentifier]);
                    }
                }
                catch (TargetInvocationException tie) when (tie.InnerException is InvalidVersionException)
                {
                    // Expected: this version isn't supported by this frame type. Skip.
                    continue;
                }
                catch (TargetInvocationException tie) when (tie.InnerException is InvalidDataException)
                {
                    // Some seed identifiers may not be valid for some versions; skip.
                    continue;
                }

                checkedPairs++;
                try
                {
                    var identifier = frame.Identifier;
                    if (string.IsNullOrEmpty(identifier))
                    {
                        failures.Add($"{type.Name} @ {version}: Identifier was null/empty.");
                    }
                }
                catch (Exception ex)
                {
                    failures.Add($"{type.Name} @ {version}: Identifier threw {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        Assert.True(failures.Count == 0,
            $"Frame identifier registry coverage gaps:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");

        // Pin: we should have checked a meaningful number of (type, version) pairs.
        Assert.True(checkedPairs >= 60,
            $"Expected at least 60 (frame type, version) pairs to be checked, got {checkedPairs}.");
    }
}
