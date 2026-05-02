using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetControlItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(actualCode, Is.EqualTo((short)code));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetControlItemMessage_EmptyParameters_ProducesMinimalMessage()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;

            // Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, Array.Empty<byte>());

            // Assert: 2 header bytes + 2 code bytes = 4
            Assert.That(msg.Length, Is.EqualTo(4));
        }

        [Test]
        public void GetControlItemMessage_TooLargeParameters_ThrowsArgumentException()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            // header(2) + code(2) + params must exceed 8191
            var tooLarge = new byte[8191];

            // Act / Assert
            Assert.Throws<ArgumentException>(
                () => NetSdrMessageHelper.GetControlItemMessage(type, code, tooLarge));
        }

        [Test]
        public void GetDataItemMessage_MaxDataItemLength_EncodesZeroInHeader()
        {
            // Arrange — DataItem edge case: total length == 8194 → header length field must be 0
            var type = NetSdrMessageHelper.MsgTypes.DataItem0;
            // 8194 - 2 header bytes = 8192 parameter bytes
            var parameters = new byte[8192];

            // Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, parameters);

            var raw = BitConverter.ToUInt16(msg.Take(2).ToArray());
            var encodedLength = raw - ((int)type << 13);

            // Assert: the special-case length is encoded as 0
            Assert.That(encodedLength, Is.EqualTo(0));
            Assert.That(msg.Length, Is.EqualTo(8194));
        }

        [Test]
        public void GetDataItemMessage_DoesNotContainControlItemCode()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem1;
            var parameters = new byte[] { 0xAA, 0xBB, 0xCC };

            // Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, parameters);

            // Assert: total = 2 header + 3 params (no 2-byte code in between)
            Assert.That(msg.Length, Is.EqualTo(5));
            Assert.That(msg.Skip(2).ToArray(), Is.EqualTo(parameters));
        }

        [Test]
        public void TranslateMessage_DataItem_RoundTrip()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            ushort sequenceNumber = 42;
            var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

            // Build manually: header + sequence number + payload
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(
                type,
                BitConverter.GetBytes(sequenceNumber).Concat(payload).ToArray());

            // Act
            bool success = NetSdrMessageHelper.TranslateMessage(
                msg, out var actualType, out _, out var actualSeq, out var body);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(actualType, Is.EqualTo(type));
            Assert.That(actualSeq, Is.EqualTo(sequenceNumber));
            Assert.That(body, Is.EqualTo(payload));
        }

        [Test]
        public void GetSamples_16Bit_ExtractsCorrectValues()
        {
            // Arrange: two 16-bit little-endian samples: 256 (0x00, 0x01) and 512 (0x00, 0x02)
            var body = new byte[] { 0x00, 0x01, 0x00, 0x02 };

            // Act
            var samples = NetSdrMessageHelper.GetSamples(16, body).ToList();

            // Assert
            Assert.That(samples.Count, Is.EqualTo(2));
            Assert.That(samples[0], Is.EqualTo(256));
            Assert.That(samples[1], Is.EqualTo(512));
        }

        [Test]
        public void GetSamples_OversizedSampleSize_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var body = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            // Act / Assert: 40-bit sample size (5 bytes) exceeds the 4-byte cap
            Assert.Throws<ArgumentOutOfRangeException>(
                () => NetSdrMessageHelper.GetSamples(40, body).ToList());
        }
    }
}