# S400 Measurement Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Capture complete dual-frequency S400 measurements, retain calibration history, and upload only validated body-composition fields to Garmin.

**Architecture:** Add a platform-independent core project for S400 packet decoding, session aggregation, estimation, calibration, and Garmin-safe value selection. Keep BLE callbacks, MAUI persistence wiring, screens, and Garmin transport in the existing app. A complete S400 result is emitted only after both 50 kHz and 250 kHz packets have been associated by profile and scale timestamp.

**Tech Stack:** .NET 8 core/test libraries consumed by the .NET 9 MAUI app, C#, MAUI, Portable.BouncyCastle, System.Text.Json, xUnit, Garmin FIT SDK through YetAnotherGarminConnectClient.

**Repository rule:** Do not create commits unless the user explicitly requests one.

---

### Task 1: Add Testable Core Projects

**Files:**
- Create: `src/MiScaleExporter.Core/MiScaleExporter.Core.csproj`
- Create: `tests/MiScaleExporter.Core.Tests/MiScaleExporter.Core.Tests.csproj`
- Create: `tests/MiScaleExporter.Core.Tests/Usings.cs`
- Modify: `src/MiScaleExporter.MAUI/MiScaleExporter.MAUI.csproj`
- Modify: `MiScaleExporter.sln`

- [ ] **Step 1: Create the core project**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Portable.BouncyCastle" Version="1.9.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create the xUnit project and reference Core**

Use `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, and `coverlet.collector`; reference `../../src/MiScaleExporter.Core/MiScaleExporter.Core.csproj`.

- [ ] **Step 3: Reference Core from MAUI and add both projects to the solution**

- [ ] **Step 4: Run the empty test suite**

Run: `dotnet test tests/MiScaleExporter.Core.Tests/MiScaleExporter.Core.Tests.csproj`

Expected: build succeeds and zero tests fail.

### Task 2: Decode S400 Advertisements

**Files:**
- Create: `src/MiScaleExporter.Core/S400/S400Packet.cs`
- Create: `src/MiScaleExporter.Core/S400/S400PacketKind.cs`
- Create: `src/MiScaleExporter.Core/S400/S400DecodeException.cs`
- Create: `src/MiScaleExporter.Core/S400/S400AdvertisementDecoder.cs`
- Create: `tests/MiScaleExporter.Core.Tests/S400/S400AdvertisementDecoderTests.cs`

- [ ] **Step 1: Write decoder tests using public captures**

```csharp
[Fact]
public void Decode_WeightPacket_ReturnsWeightLowImpedanceAndHeartRate()
{
    var packet = _decoder.Decode(
        Convert.FromHexString("4859D53B0ABC078FF2348C844138E930220000009E538599"),
        "0728974d657a4b60964c1b1677f35f7c",
        "8C:D0:B2:F6:BE:EF");

    Assert.Equal(S400PacketKind.WeightAndLowImpedance, packet.Kind);
    Assert.Equal(69.9, packet.WeightKg);
    Assert.Equal(543.2, packet.Impedance50KhzOhm);
    Assert.Equal(92, packet.HeartRateBpm);
    Assert.False(packet.IsFinal);
}

[Fact]
public void Decode_FinalPacket_ReturnsHighImpedanceAndSameTimestamp()
{
    var packet = _decoder.Decode(
        Convert.FromHexString("4859D53B0BD6EF0B25DB72785E7E2F46D6000000D8642DF6"),
        "0728974d657a4b60964c1b1677f35f7c",
        "8C:D0:B2:F6:BE:EF");

    Assert.Equal(S400PacketKind.HighImpedanceFinal, packet.Kind);
    Assert.Equal(497.6, packet.Impedance250KhzOhm);
    Assert.True(packet.IsFinal);
}
```

Also test 26-byte input, socks/weight-only input, reset input, malformed lengths, invalid hexadecimal keys, invalid MAC addresses, wrong authentication tag, object type other than `0x6E16`, and invalid Unix timestamps.

- [ ] **Step 2: Run decoder tests and verify they fail**

Run: `dotnet test tests/MiScaleExporter.Core.Tests/MiScaleExporter.Core.Tests.csproj --filter S400AdvertisementDecoderTests`

Expected: FAIL because decoder types do not exist.

- [ ] **Step 3: Implement the immutable packet contract**

```csharp
public sealed record S400Packet(
    S400PacketKind Kind,
    byte ProfileId,
    uint UnixTimestamp,
    double? WeightKg,
    int? HeartRateBpm,
    double? Impedance50KhzOhm,
    double? Impedance250KhzOhm,
    bool IsFinal,
    byte[] RawAdvertisement);
```

- [ ] **Step 4: Implement AES-CCM decoding and bit extraction**

Accept 24-byte MiBeacon data or strip the first two bytes from 26-byte service data. Validate the 16-byte key and 6-byte MAC, construct nonce `reverseMac + data[2..5] + data[^7..^4]`, authenticate with associated byte `0x11`, require object type `0x6E16` and length 9, then decode `<profileId, uint packed, uint timestamp>`.

- [ ] **Step 5: Run decoder tests**

Expected: all decoder tests pass.

### Task 3: Aggregate a Complete Measurement

**Files:**
- Create: `src/MiScaleExporter.Core/S400/S400MeasurementQuality.cs`
- Create: `src/MiScaleExporter.Core/S400/S400Measurement.cs`
- Create: `src/MiScaleExporter.Core/S400/S400MeasurementAccumulator.cs`
- Create: `tests/MiScaleExporter.Core.Tests/S400/S400MeasurementAccumulatorTests.cs`

- [ ] **Step 1: Write failing aggregation tests**

Cover first-packet preview, second-packet completion, reverse packet order, duplicate packets, mismatched profile/timestamp, reset, and socks measurement.

```csharp
var first = accumulator.Add(weightPacket);
Assert.Null(first.Completed);
Assert.Equal(69.9, first.PreviewWeightKg);

var second = accumulator.Add(highPacket);
Assert.Equal(S400MeasurementQuality.DualFrequencyComplete, second.Completed!.Quality);
Assert.Equal(543.2, second.Completed.Impedance50KhzOhm);
Assert.Equal(497.6, second.Completed.Impedance250KhzOhm);
```

- [ ] **Step 2: Run tests and verify failure**

- [ ] **Step 3: Implement aggregation keyed by profile ID and Unix timestamp**

Expose `Add(S400Packet)`, `Reset()`, and raw packet snapshots. A weight-only packet completes with `WeightOnly`; a dual-frequency result completes only when both impedance channels and weight exist.

- [ ] **Step 4: Run aggregation tests**

Expected: all aggregation tests pass.

### Task 4: Add Versioned Estimation and Calibration

**Files:**
- Create: `src/MiScaleExporter.Core/Composition/BodyProfile.cs`
- Create: `src/MiScaleExporter.Core/Composition/BodyCompositionEstimate.cs`
- Create: `src/MiScaleExporter.Core/Composition/S400BodyCompositionEstimator.cs`
- Create: `src/MiScaleExporter.Core/Calibration/CalibrationReference.cs`
- Create: `src/MiScaleExporter.Core/Calibration/FatCalibrationModel.cs`
- Create: `tests/MiScaleExporter.Core.Tests/Composition/S400BodyCompositionEstimatorTests.cs`
- Create: `tests/MiScaleExporter.Core.Tests/Calibration/FatCalibrationModelTests.cs`
- Modify: `src/MiScaleExporter.MAUI/Services/FatCalibration.cs`

- [ ] **Step 1: Write estimator tests**

Assert standard BMI, Mifflin-St Jeor BMR, finite bounded estimates, weight-only behavior, deterministic output, and algorithm version `legacy-50khz-v1`.

```csharp
Assert.Equal(21.1, estimate.Bmi, 1);
Assert.Equal(1696, estimate.BasalMetabolicRateKcal, 0);
Assert.Equal("legacy-50khz-v1", estimate.AlgorithmVersion);
```

- [ ] **Step 2: Write calibration tests**

Assert identity for zero references, no automatic correction for one or two references, mean-offset correction for three to five references, bounded linear fit for six or more sufficiently spread references, no extrapolation outside the configured margin, and fit metadata.

- [ ] **Step 3: Run tests and verify failure**

- [ ] **Step 4: Implement the estimator**

Use the existing reverse-engineered 50 kHz body-fat basis for compatibility, compute BMI directly, replace BMR with Mifflin-St Jeor, compute each dependent metric once from the calibrated fat value, and label all outputs as estimates.

- [ ] **Step 5: Implement conservative personal calibration**

Do not infer calibration from scale-only history. Apply only user-entered Xiaomi Home, DEXA, InBody, or Other references according to the sample-count rules in the design.

- [ ] **Step 6: Run estimator and calibration tests**

Expected: all tests pass without discontinuities or non-finite values over supported S400 profile ranges.

### Task 5: Integrate the Stateful S400 Path

**Files:**
- Modify: `src/MiScaleExporter.MAUI/Services/IDataInterpreter.cs`
- Modify: `src/MiScaleExporter.MAUI/Services/DataInterpreter.cs`
- Modify: `src/MiScaleExporter.MAUI/Services/Scale.cs`
- Modify: `src/MiScaleExporter.MAUI/Models/BodyComposition.cs`
- Modify: `src/MiScaleExporter.MAUI/App.xaml.cs`
- Create: `tests/MiScaleExporter.Core.Tests/Integration/S400PipelineTests.cs`

- [ ] **Step 1: Write a failing two-packet pipeline test**

The first packet must return a non-final preview; the second must return one complete composition with both impedance values, heart rate, profile ID, scale timestamp, algorithm version, and both raw packets.

- [ ] **Step 2: Add session lifecycle to the interpreter**

```csharp
public interface IDataInterpreter
{
    void ResetSession();
    BodyComposition ComputeData(byte[] data, User user, string btAddress);
}
```

Call `ResetSession()` at the start of every scan and on cancellation.

- [ ] **Step 3: Map S400 completion semantics**

Add nullable `Impedance50Khz`, `Impedance250Khz`, `HeartRate`, `ProfileId`, `MeasurementId`, `AlgorithmVersion`, and `MeasurementQuality` to `BodyComposition`. Set `HasImpedance` only for `DualFrequencyComplete`; preserve `WeightOnly` explicitly.

- [ ] **Step 4: Stop scanning only on a completed S400 result**

Do not let the 50 kHz packet or a partial estimate satisfy the current `HasImpedance` completion condition. Keep legacy scale behavior unchanged.

- [ ] **Step 5: Run pipeline and core tests**

Expected: first packet cannot finish the scan; final packet yields a complete result.

### Task 6: Persist Measurement and Calibration History

**Files:**
- Create: `src/MiScaleExporter.Core/History/MeasurementHistoryRecord.cs`
- Create: `src/MiScaleExporter.Core/History/MeasurementUploadState.cs`
- Create: `src/MiScaleExporter.Core/History/JsonMeasurementHistoryStore.cs`
- Create: `src/MiScaleExporter.Core/History/IMeasurementHistoryStore.cs`
- Create: `tests/MiScaleExporter.Core.Tests/History/JsonMeasurementHistoryStoreTests.cs`
- Modify: `src/MiScaleExporter.MAUI/App.xaml.cs`
- Modify: `src/MiScaleExporter.MAUI/Services/Scale.cs`
- Modify: `src/MiScaleExporter.MAUI/ViewModels/SettingsViewModel.cs`
- Modify: `src/MiScaleExporter.MAUI/Views/SettingsPage.xaml`

- [ ] **Step 1: Write failing persistence tests**

Test upsert by stable measurement ID, 400-day/400-record retention, atomic replacement, corrupt-file quarantine, upload-state updates, and absence of bind keys or Garmin credentials in serialized JSON.

- [ ] **Step 2: Implement the JSON store**

Use a caller-provided path, `SemaphoreSlim`, `System.Text.Json`, a schema version, a temporary file in the same directory, and atomic replacement. Store encrypted packet hex only as diagnostic evidence.

- [ ] **Step 3: Register the app-private store**

Construct it with `Path.Combine(FileSystem.AppDataDirectory, "s400-measurements.json")` and save a completed measurement before returning it from the scan.

- [ ] **Step 4: Add clear-history UI**

Add a destructive confirmation before clearing measurement and calibration history. Reset must clear both stores.

- [ ] **Step 5: Run history tests**

Expected: all persistence and retention tests pass.

### Task 7: Protect the Garmin Contract

**Files:**
- Create: `src/MiScaleExporter.Core/Garmin/GarminCompositionPayload.cs`
- Create: `src/MiScaleExporter.Core/Garmin/GarminCompositionMapper.cs`
- Create: `tests/MiScaleExporter.Core.Tests/Garmin/GarminCompositionMapperTests.cs`
- Modify: `src/MiScaleExporter.MAUI/Models/GarminBodyCompositionRequest.cs`
- Modify: `src/MiScaleExporter.MAUI/Services/GarminService.cs`
- Modify: `src/MiScaleExporter.MAUI/Views/ResultPage.xaml.cs`

- [ ] **Step 1: Write failing Garmin mapping tests**

Cover complete composition, weight-only omission, NaN/infinity rejection, supported numeric ranges, female profile gender, scale timestamp preservation, and the rule that visceral-fat rating is not visceral-fat mass.

- [ ] **Step 2: Implement one validated payload mapper**

Return required weight/timestamp and nullable optional fields. Reject invalid weight or timestamp before any network call. Set optional fields only for complete composition.

- [ ] **Step 3: Use the mapper in direct upload, external upload, and FIT export**

Set `UserProfileSettings.Gender` from the configured sex. Set `VisceralFatMass = null`. Make optional properties in `GarminBodyCompositionRequest` nullable so JSON can omit unavailable values.

- [ ] **Step 4: Add upload idempotency**

Before auto-upload, skip records already marked `Succeeded`. Persist `Succeeded` only after Garmin confirms success; persist `Failed` with a non-sensitive message otherwise.

- [ ] **Step 5: Run Garmin mapper tests**

Expected: complete and weight-only mappings contain exactly the intended fields.

### Task 8: Make Measurement Provenance Clear

**Files:**
- Modify: `src/MiScaleExporter.MAUI/ViewModels/ResultViewModel.cs`
- Modify: `src/MiScaleExporter.MAUI/Views/ResultPage.xaml`
- Modify: `src/MiScaleExporter.MAUI/Services/MetricPresenter.cs`
- Modify: `src/MiScaleExporter.MAUI/Services/MetricEvaluator.cs`
- Modify: `src/MiScaleExporter.MAUI/Resources/Localization/AppSnippets.resx`
- Modify: generated localization designer through the project generator

- [ ] **Step 1: Add view-model tests where logic is platform independent**

Assert measured/estimated provenance text, weight-only messaging, impedance formatting, and omission of misleading additive composition segments.

- [ ] **Step 2: Update result semantics**

Show heart rate and both impedance readings in details. Label body composition as estimated. Rename muscle and bone labels to make their estimated meaning explicit. Replace the additive donut with a non-overlapping fat-mass versus fat-free-mass presentation.

- [ ] **Step 3: Make status bands conservative**

Keep BMI classification. Mark unsupported or overly coarse bone/muscle/metabolic-age classifications as informational rather than clinical low/standard/high judgments.

- [ ] **Step 4: Build the MAUI project**

Run: `dotnet build src/MiScaleExporter.MAUI/MiScaleExporter.MAUI.csproj -c Debug -f net9.0-android`

Expected: build succeeds with no new warnings attributable to these changes.

### Task 9: End-to-End Verification

**Files:**
- Modify only files required by failures found in this task.

- [ ] **Step 1: Run the complete unit suite**

Run: `dotnet test tests/MiScaleExporter.Core.Tests/MiScaleExporter.Core.Tests.csproj -c Release`

Expected: all tests pass.

- [ ] **Step 2: Build Debug and Release Android outputs**

Run: `dotnet build src/MiScaleExporter.MAUI/MiScaleExporter.MAUI.csproj -c Debug -f net9.0-android`

Run: `dotnet build src/MiScaleExporter.MAUI/MiScaleExporter.MAUI.csproj -c Release -f net9.0-android`

Expected: both builds succeed.

- [ ] **Step 3: Inspect the focused diff**

Run: `git diff --check`

Run: `git diff --stat`

Expected: no whitespace errors and no unrelated files changed.

- [ ] **Step 4: Report device-only checks explicitly**

On a physical S400, verify that the scan remains active after the 50 kHz packet, completes on the 250 kHz packet, displays the scale timestamp and both impedances, persists one history record, and creates one Garmin entry. Weight-only measurement must upload no composition fields.
