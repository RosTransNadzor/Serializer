using SerializerTests.Nodes;

namespace TestSerializer.Node;

public static class ListNodeExtension
{
    public static int GetListLength(this ListNode node)
    {
        ListNode? current = node;
        int length = 1;

        while ((current = current.Next) is not null)
            length++;

        return length;
    }
}