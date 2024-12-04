using SerializerTests.Interfaces;
using SerializerTests.Nodes;
using TestSerializer;
using TestSerializer.Node;

namespace SerializerTests.Implementations;

public class ExpressSerializer : IListSerializer
{
    /// <summary>
    /// Creates a deep copy of the linked list, including all nodes and their references.
    /// </summary>
    public Task<ListNode> DeepCopy(ListNode head)
    {
        Dictionary<ListNode, ListNode> nodeTransfer = new();
        ListNode? current = head;
        do
        {
            ListNode newNode;

            if (nodeTransfer.TryGetValue(current, out var value))
                newNode = value;
            else
            {
                newNode = new ListNode
                {
                    Data = current.Data
                };
                nodeTransfer[current] = newNode;
            }

            if (current.Next is not null)
            {
                if (!nodeTransfer.ContainsKey(current.Next))
                    nodeTransfer[current.Next] = new ListNode
                    {
                        Data = current.Next.Data
                    };

                newNode.Next = nodeTransfer[current.Next];
                newNode.Next.Previous = newNode;
            }

            if (!nodeTransfer.ContainsKey(current.Random))
            {
                nodeTransfer[current.Random] = new ListNode
                {
                    Data = current.Random.Data
                };
                newNode.Random = nodeTransfer[current.Random];
            }
                
        } while ((current = current.Next) is not null);

        return Task.FromResult(nodeTransfer[head]);
    }

    /// <summary>
    /// Deserializes a linked list from a stream and reconstructs all nodes and their references.
    /// </summary>
    public async Task<ListNode> Deserialize(Stream stream)
    {
        int listLength = await ReadListLengthAsync(stream);
        INodeSerializer nodeDeserializer = new BinaryNodeSerializer(stream);

        SerializedNode[] serializedNodes = new SerializedNode[listLength];

        // Deserialize nodes
        for (int i = 0; i < listLength; i++)
        {
            serializedNodes[i] = await nodeDeserializer.DeserializeNodeAsync();
        }

        // Create list nodes and restore links
        ListNode[] nodes = new ListNode[serializedNodes.Length];
        foreach (var serializedNode in serializedNodes)
        {
            nodes[serializedNode.Id - 1] = new ListNode
            {
                Data = serializedNode.Data
            };
        }

        return RestoreListLinks(nodes, serializedNodes);
    }
        
    public async Task Serialize(ListNode head, Stream stream)
    {
        int listLength = head.GetListLength();
        await WriteListLengthAsync(listLength, stream);

        NodeIdsMapper mapper = new(listLength);
        INodeSerializer nodeSerializer = new BinaryNodeSerializer(stream);
        SerializedNode serializedNode = new SerializedNode();

        ListNode? current = head;

        do
        {
            mapper.GetOrAddId(current); 
            mapper.GetOrAddId(current.Random);

            if (current.Next is not null)
                mapper.GetOrAddId(current.Next);

            serializedNode.Fill(mapper, current);
            await nodeSerializer.SerializeNodeAsync(serializedNode);

        } while ((current = current.Next) is not null);

        // Ensure all data is written to the stream
        await stream.FlushAsync(); 
    }

    /// <summary>
    /// Serializes list metadata (length and max data size) into the stream.
    /// </summary>
    private async Task WriteListLengthAsync(int listLength, Stream stream)
    {
        await stream.WriteAsync(BitConverter.GetBytes(listLength));
    }

    /// <summary>
    /// Restores references (Previous, Next, Random) between nodes based on serialized data.
    /// </summary>
    private ListNode RestoreListLinks(ListNode[] nodes, SerializedNode[] serializedNodes)
    {
        foreach (var serializedNode in serializedNodes)
        {
            ListNode node = nodes[serializedNode.Id - 1];

            node.Random = nodes[serializedNode.RandomId - 1];
            if (serializedNode.PrevId != 0)
                node.Previous = nodes[serializedNode.PrevId - 1];
            if (serializedNode.NextId != 0)
                node.Next = nodes[serializedNode.NextId - 1];
        }

        // Return the head of the restored list
        return nodes[0];
    }

    /// <summary>
    /// Reads list length
    /// </summary>
    private async Task<int> ReadListLengthAsync(Stream stream)
    {
        byte[] bytes = new byte[4];

        // Read list length
        int bytesRead = await stream.ReadAsync(bytes, 0, 4);
        if (bytesRead != 4)
            throw new InvalidOperationException("Unexpected end of stream while reading metadata.");
            
        int listLength = BitConverter.ToInt32(bytes);

        return listLength;
    }
}