using System.Buffers;
using System.Text;

namespace TestSerializer;

public interface INodeSerializer
{
    /// <summary>
    /// Serializes a single node into the stream.
    /// </summary>
    /// <param name="node">The node to serialize.</param>
    Task SerializeNodeAsync(SerializedNode node);
    /// <summary>
    /// Deserializes a single node from the stream asynchronously.
    /// </summary>
    Task<SerializedNode> DeserializeNodeAsync();
}

public class BinaryNodeSerializer : INodeSerializer
{
    private const int BufferLength = 4096; 
    private readonly Stream _stream;
    // Buffer to write data before sending to the stream
    private readonly ArrayBufferWriter<byte> _buffer; 
    
    public BinaryNodeSerializer(Stream stream)
    {
        _stream = stream;
        _buffer = new ArrayBufferWriter<byte>(BufferLength);
    }

    /// <summary>
    /// Serializes the provided node into the stream asynchronously.
    /// </summary>
    public async Task SerializeNodeAsync(SerializedNode node)
    {
        SerializeInt(node.Id);
        SerializeInt(node.PrevId);
        SerializeInt(node.NextId);
        SerializeInt(node.RandomId);
        // Length of data string
        SerializeInt(Encoding.UTF8.GetByteCount(node.Data)); 
        
        await SerializeDataAsync(node.Data);
    }

    /// <summary>
    /// Serializes an integer to the buffer.
    /// </summary>
    private void SerializeInt(int value)
    {
        Span<byte> freeMemory = _buffer.GetSpan();
        BitConverter.TryWriteBytes(freeMemory, value);
        _buffer.Advance(4);
    }

    /// <summary>
    /// Writes the buffered data to the stream asynchronously.
    /// </summary>
    private async Task WriteBufferToStreamAsync()
    {
        var memory = _buffer.WrittenMemory;
        await _stream.WriteAsync(memory);
        
        // Reset the written count of the buffer for the next write
        _buffer.ResetWrittenCount();
    }

    /// <summary>
    /// Serializes the string data into the stream.
    /// </summary>
    private async Task SerializeDataAsync(string data)
    {
        // If there is not enough free space to read 1 char (4 bytes),write the data to the stream
        if (_buffer.FreeCapacity < 4) await WriteBufferToStreamAsync();
        
        var encoder = Encoding.UTF8.GetEncoder();

        ReadOnlyMemory<char> leftConvert = data.AsMemory();
        bool isCompleted = false;
        Memory<byte> bufferMemory = _buffer.GetMemory();
        
        while (!isCompleted)
        {
            encoder.Convert(
                chars: leftConvert.Span,
                bytes: bufferMemory.Span,
                false,
                out int charsUsed,
                out int bytesUsed,
                out isCompleted
            );
            
            leftConvert = leftConvert.Slice(charsUsed);
            
            _buffer.Advance(bytesUsed);

            // If there is not enough free space to read 1 char (4 bytes),write the data to the stream
            if (_buffer.FreeCapacity < 4) await WriteBufferToStreamAsync();
            
            bufferMemory = _buffer.GetMemory();
        }

        // Write any remaining data in the buffer to the stream
        if (_buffer.WrittenCount != 0) await WriteBufferToStreamAsync();
    }
    
    /// <summary>
    /// Deserializes a single node from the stream asynchronously.
    /// </summary>
    public async Task<SerializedNode> DeserializeNodeAsync()
    {
        // Read the node's metadata (IDs) and data length
        int id = await ReadIntAsync();
        int prevId = await ReadIntAsync();
        int nextId = await ReadIntAsync();
        int randomId = await ReadIntAsync();
        int dataLength = await ReadIntAsync();

        // The buffer might need to be resized for larger data
        var freeMemory = _buffer.GetMemory(dataLength);

        int bytesRead = await _stream.ReadAsync(freeMemory.Slice(0,dataLength));

        if (bytesRead != dataLength)
        {
            throw new InvalidOperationException("Not enough data read for the node's data.");
        }
        _buffer.Advance(dataLength);
        
        string data = Encoding.UTF8.GetString(_buffer.WrittenSpan);

        // Reset the buffer for reuse
        _buffer.ResetWrittenCount();

        return new SerializedNode
        {
            Data = data,
            Id = id,
            PrevId = prevId,
            NextId = nextId,
            RandomId = randomId
        };
    }

    /// <summary>
    /// Reads an integer (4 bytes) from the stream asynchronously.
    /// </summary>
    private async Task<int> ReadIntAsync()
    {
        var buffer = new byte[4];

        int bytesRead = await _stream.ReadAsync(buffer, 0, 4);

        if (bytesRead != 4)
            throw new InvalidOperationException("There is not enough data to read an integer.");
        
        return BitConverter.ToInt32(buffer);
    }
}
