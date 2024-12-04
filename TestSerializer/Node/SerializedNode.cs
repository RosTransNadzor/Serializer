using SerializerTests.Nodes;
using TestSerializer.Node;

namespace TestSerializer;

public record SerializedNode
{
    public int Id { get; set; }
    public int NextId { get; set; }
    public int PrevId { get; set; }
    public int RandomId { get; set; }
    public string Data { get; set; } = string.Empty;

    public void Fill(NodeIdsMapper mapper, ListNode node)
    {
        Data = node.Data;
        Id = mapper.GetOrAddId(node);
        PrevId = node.Previous is null ? 0 : mapper.GetOrAddId(node.Previous);
        NextId = node.Next is null ? 0 : mapper.GetOrAddId(node.Next);
        RandomId = mapper.GetOrAddId(node.Random);
    }
}