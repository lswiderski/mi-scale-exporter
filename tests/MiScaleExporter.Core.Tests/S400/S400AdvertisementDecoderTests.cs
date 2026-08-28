using MiScaleExporter.Core.S400;

namespace MiScaleExporter.Core.Tests.S400;

public sealed class S400AdvertisementDecoderTests
{
    private const string BindKey = "0728974d657a4b60964c1b1677f35f7c";
    private const string MacAddress = "8C:D0:B2:F6:BE:EF";
    private const string WeightPacket = "4859D53B0ABC078FF2348C844138E930220000009E538599";
    private const string FinalPacket = "4859D53B0BD6EF0B25DB72785E7E2F46D6000000D8642DF6";

    [Fact]
    public void Decode_WeightPacket_ReturnsWeightLowImpedanceAndHeartRate()
    {
        var decoder = new S400AdvertisementDecoder();

        var packet = decoder.Decode(
            Convert.FromHexString(WeightPacket),
            BindKey,
            MacAddress);

        Assert.Equal(S400PacketKind.WeightAndLowImpedance, packet.Kind);
        Assert.Equal((byte)1, packet.ProfileId);
        Assert.Equal(69.9, packet.WeightKg);
        Assert.Equal(543.2, packet.Impedance50KhzOhm);
        Assert.Null(packet.Impedance250KhzOhm);
        Assert.Equal(92, packet.HeartRateBpm);
        Assert.False(packet.IsFinal);
    }

    [Fact]
    public void Decode_FinalPacket_ReturnsHighImpedanceAndSameTimestamp()
    {
        var decoder = new S400AdvertisementDecoder();
        var first = decoder.Decode(Convert.FromHexString(WeightPacket), BindKey, MacAddress);

        var final = decoder.Decode(Convert.FromHexString(FinalPacket), BindKey, MacAddress);

        Assert.Equal(S400PacketKind.HighImpedanceFinal, final.Kind);
        Assert.Equal(first.ProfileId, final.ProfileId);
        Assert.Equal(first.UnixTimestamp, final.UnixTimestamp);
        Assert.Null(final.WeightKg);
        Assert.Null(final.HeartRateBpm);
        Assert.Null(final.Impedance50KhzOhm);
        Assert.Equal(497.6, final.Impedance250KhzOhm);
        Assert.True(final.IsFinal);
    }

    [Fact]
    public void Decode_ServiceDataWithUuidPrefix_StripsPrefix()
    {
        var decoder = new S400AdvertisementDecoder();

        var packet = decoder.Decode(
            Convert.FromHexString("95FE" + WeightPacket),
            BindKey,
            MacAddress);

        Assert.Equal(69.9, packet.WeightKg);
    }

    [Fact]
    public void Decode_WeightOnlyPacket_ReturnsFinalWithoutImpedance()
    {
        var decoder = new S400AdvertisementDecoder();

        var packet = decoder.Decode(
            Convert.FromHexString("4859D53B71530438B5894B242C209908DA000000479ECDA3"),
            "02d2900363ef629c736a4549677acbee",
            "04:AE:47:67:C6:7C");

        Assert.Equal(S400PacketKind.WeightOnly, packet.Kind);
        Assert.Equal(74.7, packet.WeightKg);
        Assert.Null(packet.Impedance50KhzOhm);
        Assert.Null(packet.Impedance250KhzOhm);
        Assert.True(packet.IsFinal);
    }

    [Fact]
    public void Decode_ResetPacket_ReturnsReset()
    {
        var decoder = new S400AdvertisementDecoder();

        var packet = decoder.Decode(
            Convert.FromHexString("4859D53B72036C6794355A19DBC864BFB3000000E4151DC8"),
            "02d2900363ef629c736a4549677acbee",
            "04:AE:47:67:C6:7C");

        Assert.Equal(S400PacketKind.Reset, packet.Kind);
        Assert.Null(packet.WeightKg);
        Assert.False(packet.IsFinal);
    }

    [Theory]
    [InlineData(23)]
    [InlineData(25)]
    [InlineData(27)]
    public void Decode_InvalidLength_Throws(int length)
    {
        var decoder = new S400AdvertisementDecoder();

        var exception = Assert.Throws<S400DecodeException>(() =>
            decoder.Decode(new byte[length], BindKey, MacAddress));

        Assert.Contains("24 or 26 bytes", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-hex")]
    [InlineData("0728974d657a4b60964c1b1677f35f")]
    public void Decode_InvalidBindKey_Throws(string bindKey)
    {
        var decoder = new S400AdvertisementDecoder();

        var exception = Assert.Throws<S400DecodeException>(() =>
            decoder.Decode(Convert.FromHexString(WeightPacket), bindKey, MacAddress));

        Assert.Contains("bind key", exception.Message);
    }

    [Fact]
    public void Decode_WrongBindKey_ThrowsAuthenticationFailure()
    {
        var decoder = new S400AdvertisementDecoder();

        var exception = Assert.Throws<S400DecodeException>(() =>
            decoder.Decode(
                Convert.FromHexString(WeightPacket),
                "00000000000000000000000000000000",
                MacAddress));

        Assert.Contains("authentication failed", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("8C:D0:B2:F6:BE")]
    [InlineData("8C:D0:B2:F6:BE:GG")]
    public void Decode_InvalidMacAddress_Throws(string macAddress)
    {
        var decoder = new S400AdvertisementDecoder();

        var exception = Assert.Throws<S400DecodeException>(() =>
            decoder.Decode(Convert.FromHexString(WeightPacket), BindKey, macAddress));

        Assert.Contains("Bluetooth address", exception.Message);
    }

    [Fact]
    public void Decode_ServiceDataWithWrongUuid_Throws()
    {
        var decoder = new S400AdvertisementDecoder();
        var data = Convert.FromHexString("94FE" + WeightPacket);

        var exception = Assert.Throws<S400DecodeException>(() =>
            decoder.Decode(data, BindKey, MacAddress));

        Assert.Contains("FE95", exception.Message);
    }

    [Fact]
    public void Decode_ZeroTimestamp_Throws()
    {
        var decoder = new S400AdvertisementDecoder();
        var valid = decoder.Decode(Convert.FromHexString(WeightPacket), BindKey, MacAddress);

        var exception = Assert.Throws<S400DecodeException>(() =>
            decoder.ValidateTimestamp(0));

        Assert.Contains("timestamp", exception.Message);
        Assert.True(valid.UnixTimestamp > 0);
    }

    [Fact]
    public void Decode_WrongProductId_RejectsBeforeDecryption()
    {
        var data = Convert.FromHexString(WeightPacket);
        data[2] = 0;
        data[3] = 0;
        var decoder = new S400AdvertisementDecoder();

        var exception = Assert.Throws<S400DecodeException>(() =>
            decoder.Decode(data, BindKey, MacAddress));

        Assert.Contains("product", exception.Message);
    }

    [Theory]
    [InlineData(0x30D9)]
    [InlineData(0x3BD5)]
    [InlineData(0x48CF)]
    [InlineData(0x4B05)]
    public void IsSupportedProductId_KnownS400Variant_ReturnsTrue(int productId)
    {
        Assert.True(S400AdvertisementDecoder.IsSupportedProductId((ushort)productId));
    }
}
