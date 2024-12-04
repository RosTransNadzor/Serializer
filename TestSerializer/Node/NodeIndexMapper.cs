using SerializerTests.Nodes;

namespace TestSerializer.Node;

public class NodeIdsMapper
{
    private readonly Dictionary<ListNode, int> _ids;

    public NodeIdsMapper(int listLength)
    {
        _ids = new(listLength);
    }
    private int _currentId = 1;

    public int GetOrAddId(ListNode node)
    {
        if (!_ids.ContainsKey(node))
        {
            _ids[node] = _currentId++;
        }
        return _ids[node];
    }
}