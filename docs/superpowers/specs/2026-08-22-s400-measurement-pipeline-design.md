# S400 Measurement and Garmin Reliability Design

## Status

Approved for implementation based on the user's request to replace the current S400 path while preserving reliable Garmin synchronization. No Xiaomi cloud dependency is required.

## Goals

- Decode the complete Xiaomi S400 measurement sequence locally.
- Retain weight, 50 kHz impedance, 250 kHz impedance, heart rate, profile ID, scale timestamp, and raw packets.
- Complete a body-composition measurement only after the S400 final packet arrives.
- Treat a stabilized weight-only measurement as lacking impedance.
- Keep Garmin direct upload, external API upload, and FIT export compatible and reliable.
- Persist recent S400 measurements in app-private storage for trend analysis, calibration, audit, and upload state.
- Clearly distinguish measured values from estimated values.
- Preserve legacy Mi Scale support.

## Non-goals

- Reproduce Xiaomi's proprietary dual-frequency body-composition algorithm without validation data.
- Require Xiaomi Home credentials or network access.
- Claim medical or DEXA-equivalent accuracy.
- Change Garmin authentication behavior.

## Considered Approaches

### 1. Patch the existing NuGet integration

Keep `MiScaleBodyComposition.S400Scale` and hold one instance across advertisements.

This is small, but the package misnames the second impedance channel, does not expose a stable packet/session contract, and has no body-composition validation tests. It would leave protocol state coupled to an estimator designed for older scales.

### 2. Local S400 pipeline with explicit estimation (selected)

Decode and aggregate S400 packets locally, persist complete raw measurements, and invoke body-composition estimation only after the measurement is complete. Keep the existing single-frequency formula temporarily as an explicitly versioned legacy estimate, with optional personal calibration.

This is offline, testable, transparent, and preserves Garmin compatibility without inventing an unvalidated dual-frequency formula.

### 3. Xiaomi Home as the source of truth

Fetch Xiaomi's calculated measurements from its cloud and forward them to Garmin.

This provides the closest match to Xiaomi Home but adds credentials, regional API behavior, network dependency, and vendor lock-in. It can be added later as an optional reference importer.

## Architecture

### Core S400 protocol types

Create a platform-independent `MiScaleExporter.Core` project containing:

- `S400AdvertisementDecoder`: validates and decrypts one MiBeacon advertisement.
- `S400Packet`: immutable decoded packet containing profile ID, timestamp, optional weight, optional heart rate, optional impedance frequency/value, and raw bytes.
- `S400MeasurementAccumulator`: combines packets belonging to the same profile ID and timestamp.
- `S400Measurement`: immutable completed or weight-only measurement.
- `S400MeasurementQuality`: `Incomplete`, `WeightOnly`, or `DualFrequencyComplete`.

The decoder accepts both 24-byte MiBeacon payloads and the 26-byte service-data form currently returned by Plugin.BLE. It uses AES-CCM with the configured 16-byte bind key and Bluetooth MAC address.

### Packet semantics

The decrypted object is Xiaomi object `0x6E16`, with a 9-byte value:

- byte 0: profile ID
- bytes 1-4: packed measurement data
- bytes 5-8: Unix timestamp

Packed measurement data:

- bits 0-10: weight in 0.1 kg units
- bits 11-17: heart rate minus 50
- bits 18-31: impedance in 0.1 ohm units

Packet interpretation:

- Weight present and impedance present: weight, heart rate, and 50 kHz impedance; measurement is not complete.
- No weight and no heart rate, impedance present: 250 kHz impedance; measurement is complete.
- Weight present and impedance absent: stabilized weight-only measurement, normally caused by no electrode contact.
- All values zero: reset/stepped-off signal.

The accumulator is keyed by profile ID and scale timestamp, tolerates duplicate and out-of-order advertisements, and resets on a newer timestamp, explicit reset packet, cancellation, or scan start.

## Data Flow

1. `Scale` receives all relevant MiBeacon service-data records rather than taking an arbitrary first service-data record.
2. `DataInterpreter` routes legacy scales to the existing package and S400 packets to the local decoder and accumulator.
3. A 50 kHz packet produces only a weight preview and keeps scanning.
4. A 250 kHz packet completes the measurement and provides both impedances.
5. A weight-only packet completes as `WeightOnly` with `HasImpedance = false`.
6. A complete measurement is passed to the estimator, validated, persisted, displayed, and optionally uploaded.
7. Garmin receives the scale timestamp and only valid, present fields.

## Body Composition Policy

### Measured values

- Weight
- 50 kHz impedance
- 250 kHz impedance
- Heart rate
- Profile ID
- Scale timestamp

### Estimated values

- BMI
- Body fat percentage
- Body water percentage
- Lean soft mass currently labeled muscle mass
- Estimated bone mass
- Visceral fat rating
- BMR
- Metabolic age
- Ideal weight
- Physique/body type
- Protein percentage

The initial estimator remains compatible with the existing reverse-engineered formula and uses 50 kHz impedance. It is named and persisted as `legacy-50khz-v1`. The 250 kHz value is retained but is not used to claim extra accuracy until a model is validated against reference data.

BMI remains the standard `weight / height^2` calculation. BMR moves to Mifflin-St Jeor. Visceral fat, metabolic age, bone mass, and physique rating remain explicitly labeled estimates. The UI must not imply that lean soft mass is independently measured skeletal muscle or that estimated bone mass is bone mineral density.

## Calibration

Recent measurements are retained so a later reference can be paired with the original raw inputs.

A calibration reference records:

- measurement ID
- reference date and time
- reference source: Xiaomi Home, DEXA, InBody, or Other
- reference body-fat percentage
- captured weight and both impedance values
- baseline algorithm version and baseline fat estimate

Calibration behavior:

- No reference points: identity correction.
- One or two points: show the references but do not alter uploads automatically.
- Three to five points: use a robust mean offset correction.
- Six or more points with sufficient body-fat spread: use bounded linear regression.
- Never extrapolate beyond a conservative margin around the reference range.
- Store fit version, point count, fit quality, and applied correction with each measurement.

This calibration improves personal agreement with the chosen reference. It does not turn the scale into a medical device and does not infer truth from repeated scale-only measurements.

## Persistence

Store up to 400 recent measurements or 400 days, whichever is reached first, in an app-private versioned JSON file under `FileSystem.AppDataDirectory`.

Each record contains:

- stable measurement ID derived from profile ID, scale timestamp, and weight
- raw S400 values and encrypted packet hex
- calculated composition and algorithm version
- calibration version and correction metadata
- Garmin upload state: pending, succeeded, or failed
- last upload attempt and non-sensitive error summary

Writes use a lock and atomic temporary-file replacement. Corrupt files are quarantined instead of silently overwritten. Bind keys and Garmin credentials are never stored in measurement history. Settings provide export and clear-history actions.

## Garmin Contract

Create one mapper and validator used by direct upload, external API upload, and FIT export.

Rules:

- Weight and timestamp are required.
- Use the scale timestamp in UTC.
- Set the FIT user profile gender from the configured user rather than relying on the current male default.
- Omit unavailable composition fields using nullable DTO values; never convert missing values to zero.
- Upload composition only for a complete and valid impedance measurement.
- Do not write visceral-fat rating into the distinct visceral-fat-mass field.
- Validate finite values and FIT-compatible ranges before conversion to `byte` or `float`.
- Persist upload success by measurement ID so automatic navigation cannot upload an already-successful measurement again.
- A failed upload remains retryable and never destroys the local measurement.

Protein and BMR remain visible locally because the Garmin weight-scale FIT record used by the current client does not contain matching fields.

## UI Behavior

- During the first S400 packet: show stabilized weight and continue with a "reading composition" state.
- On the final packet: show the completed result.
- For weight-only measurements: show weight and BMI, explain that electrode contact was missing, and upload only those fields.
- Add a compact provenance label such as `Measured: weight, 50/250 kHz impedance; estimated: composition`.
- Rename misleading labels where necessary and avoid an additive donut that treats water as separate from lean tissue.
- Expose heart rate and both impedance readings in details/debug information.

## Error Handling

- Invalid bind key, MAC, packet length, object type, authentication tag, or timestamp produces a typed decode failure and keeps scanning where recovery is possible.
- A partial measurement expires after a short session timeout without being uploaded as full composition.
- Duplicate packets are idempotent.
- Upload mapping failures are shown before network activity.
- History persistence failures are logged and surfaced without discarding the in-memory measurement.

## Testing

Add a platform-independent test project covering:

- Public S400 packet 1: 69.9 kg, 543.2 ohm at 50 kHz, 92 bpm, not complete.
- Public S400 packet 2: 497.6 ohm at 250 kHz, same timestamp, complete.
- Both packet orders and duplicate packets.
- Weight-only/socks packet.
- Reset packet and new timestamp behavior.
- Wrong key, wrong MAC, malformed length, and authentication failure.
- Measurement persistence, retention, corruption quarantine, and migration.
- Calibration thresholds, bounds, insufficient data, and versioning.
- Garmin mapping for complete and weight-only measurements, female gender, timestamp preservation, null omission, numeric bounds, and visceral-fat rating/mass separation.
- Regression coverage for legacy scale paths.

## Rollout

Existing preference-based fat calibration points are migrated as unpaired legacy references and remain disabled for automatic correction until enough valid references exist. Existing Garmin credentials and upload settings are unchanged. Existing previous-measurement cache remains readable during migration.
