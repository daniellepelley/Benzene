using System;
using System.Text;
using Benzene.Aws.Lambda.Kinesis;
using Xunit;

namespace Benzene.Test.Aws.Kinesis;

public class KinesisRecordDataTest
{
    [Fact]
    public void GetData_DecodesBase64ToRawBytes()
    {
        var record = new KinesisRecordData { Data = Convert.ToBase64String(new byte[] { 1, 2, 3 }) };

        Assert.Equal(new byte[] { 1, 2, 3 }, record.GetData());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetData_NullOrEmptyData_ReturnsEmptyArray(string data)
    {
        var record = new KinesisRecordData { Data = data };

        Assert.Empty(record.GetData());
    }

    [Fact]
    public void GetDataAsString_DecodesBase64AsUtf8()
    {
        var record = new KinesisRecordData { Data = Convert.ToBase64String(Encoding.UTF8.GetBytes("hello")) };

        Assert.Equal("hello", record.GetDataAsString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetDataAsString_NullOrEmptyData_ReturnsEmptyString(string data)
    {
        var record = new KinesisRecordData { Data = data };

        Assert.Equal(string.Empty, record.GetDataAsString());
    }
}
