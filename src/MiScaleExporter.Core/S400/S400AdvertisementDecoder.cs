using System.Buffers.Binary;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace MiScaleExporter.Core.S400;

public sealed class S400AdvertisementDecoder
{
    private const int MiBeaconLength = 24;
    private const uint MinimumTimestamp = 1_577_836_800;
    private const ushort BodyCompositionObjectType = 0x6E16;
    private static readonly HashSet<ushort> SupportedProductIds =
    [
        0x30D9,
        0x3BD5,
        0x48CF,
        0x4B05,
    ];
    private static readonly byte[] AssociatedData = [0x11];
    private readonly TimeProvider _timeProvider;

    public S400AdvertisementDecoder(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public S400Packet Decode(byte[] advertisement, string bindKey, string macAddress)
    {
        ArgumentNullException.ThrowIfNull(advertisement);

        if (advertisement.Length == MiBeaconLength + 2
            && (advertisement[0] != 0x95 || advertisement[1] != 0xFE))
        {
            throw new S400DecodeException("S400 26-byte service data must start with the FE95 service UUID.");
        }

        var data = advertisement.Length == MiBeaconLength + 2
            ? advertisement.AsSpan(2).ToArray()
            : advertisement.ToArray();

        if (data.Length != MiBeaconLength)
        {
            throw new S400DecodeException("S400 service data must contain 24 or 26 bytes.");
        }
        if (!IsSupportedProductId(BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(2, 2))))
        {
            throw new S400DecodeException("Bluetooth advertisement is not from an S400 product.");
        }

        var key = ParseHex(bindKey, 16, "bind key");
        var mac = ParseMacAddress(macAddress);

        var nonce = new byte[12];
        for (var index = 0; index < mac.Length; index++)
        {
            nonce[index] = mac[mac.Length - index - 1];
        }
        data.AsSpan(2, 3).CopyTo(nonce.AsSpan(6));
        data.AsSpan(data.Length - 7, 3).CopyTo(nonce.AsSpan(9));

        var encryptedPayload = data.AsSpan(5, data.Length - 12).ToArray();
        var authenticatedCipherText = new byte[encryptedPayload.Length + 4];
        encryptedPayload.CopyTo(authenticatedCipherText, 0);
        data.AsSpan(data.Length - 4, 4).CopyTo(authenticatedCipherText.AsSpan(encryptedPayload.Length));

        byte[] decrypted;
        try
        {
            var cipher = new CcmBlockCipher(new AesEngine());
            cipher.Init(false, new AeadParameters(new KeyParameter(key), 32, nonce, AssociatedData));
            decrypted = new byte[cipher.GetOutputSize(authenticatedCipherText.Length)];
            var length = cipher.ProcessBytes(authenticatedCipherText, 0, authenticatedCipherText.Length, decrypted, 0);
            length += cipher.DoFinal(decrypted, length);
            if (length != decrypted.Length)
            {
                Array.Resize(ref decrypted, length);
            }
        }
        catch (InvalidCipherTextException exception)
        {
            throw new S400DecodeException("S400 advertisement authentication failed.", exception);
        }

        if (decrypted.Length != 12
            || BinaryPrimitives.ReadUInt16LittleEndian(decrypted.AsSpan(0, 2)) != BodyCompositionObjectType
            || decrypted[2] != 9)
        {
            throw new S400DecodeException("S400 advertisement does not contain a 0x6E16 body-composition object.");
        }

        var profileId = decrypted[3];
        var packed = BinaryPrimitives.ReadUInt32LittleEndian(decrypted.AsSpan(4, 4));
        var unixTimestamp = BinaryPrimitives.ReadUInt32LittleEndian(decrypted.AsSpan(8, 4));
        ValidateTimestamp(unixTimestamp);
        var mass = packed & 0x7FF;
        var encodedHeartRate = (packed >> 11) & 0x7F;
        var impedance = packed >> 18;

        var kind = mass switch
        {
            0 when encodedHeartRate == 0 && impedance == 0 => S400PacketKind.Reset,
            0 when encodedHeartRate == 0 => S400PacketKind.HighImpedanceFinal,
            _ when impedance == 0 => S400PacketKind.WeightOnly,
            _ => S400PacketKind.WeightAndLowImpedance,
        };

        return new S400Packet(
            kind,
            profileId,
            unixTimestamp,
            mass == 0 ? null : mass / 10.0,
            encodedHeartRate is > 0 and < 127 ? (int)encodedHeartRate + 50 : null,
            kind == S400PacketKind.WeightAndLowImpedance ? impedance / 10.0 : null,
            kind == S400PacketKind.HighImpedanceFinal ? impedance / 10.0 : null,
            kind is S400PacketKind.WeightOnly or S400PacketKind.HighImpedanceFinal,
            advertisement.ToArray());
    }

    internal void ValidateTimestamp(uint unixTimestamp)
    {
        var maximum = _timeProvider.GetUtcNow().AddDays(1).ToUnixTimeSeconds();
        if (unixTimestamp < MinimumTimestamp || unixTimestamp > maximum)
        {
            throw new S400DecodeException("S400 measurement timestamp is outside the supported range.");
        }
    }

    public static bool IsSupportedProductId(ushort productId) =>
        SupportedProductIds.Contains(productId);

    private static byte[] ParseHex(string value, int byteCount, string fieldName)
    {
        try
        {
            var bytes = Convert.FromHexString(value ?? string.Empty);
            if (bytes.Length != byteCount)
            {
                throw new FormatException();
            }
            return bytes;
        }
        catch (FormatException exception)
        {
            throw new S400DecodeException($"S400 {fieldName} must contain exactly {byteCount * 2} hexadecimal characters.", exception);
        }
    }

    private static byte[] ParseMacAddress(string value)
    {
        var normalized = (value ?? string.Empty).Replace(":", string.Empty, StringComparison.Ordinal);
        return ParseHex(normalized, 6, "Bluetooth address");
    }
}