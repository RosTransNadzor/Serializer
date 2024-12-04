using SerializerTests.Implementations;
using SerializerTests.Interfaces;
using SerializerTests.Nodes;

namespace TestSerializer;

public static class Program
{
    public static async Task Main(string[] args)
    {
        ListNode head = InitializeList();
        
        IListSerializer serializer = new ExpressSerializer();
        
        using MemoryStream stream = new MemoryStream();
        
        await serializer.Serialize(head, stream);
        stream.Seek(0, SeekOrigin.Begin);
        
        ListNode deserializedHead = await serializer.Deserialize(stream);
        
        bool areEqual = CompareLists(head, deserializedHead);
        Console.WriteLine($"Serialization test passed: {areEqual}");
        
        ListNode deepCopyHead = await serializer.DeepCopy(head);
        
        bool areCopiedEqual = CompareLists(head, deepCopyHead);
        Console.WriteLine($"Deep copy test passed: {areCopiedEqual}");
    }
    
    private static ListNode InitializeList()
    {
        ListNode head = new ListNode { Data = "1"};
        ListNode second = new ListNode { Data = "2"};
        ListNode third = new ListNode { Data = "3"};
        ListNode fourth = new ListNode { Data = "4"};
        ListNode fifth = new ListNode { Data = "5"};

        head.Next = second;
        second.Previous = head;
        second.Next = third;
        third.Previous = second;
        third.Next = fourth;
        fourth.Previous = third;
        fourth.Next = fifth;
        fifth.Previous = fourth;
        
        head.Random = third;
        second.Random = fifth;
        third.Random = second;
        fourth.Random = head;
        fifth.Random = fourth;

        return head;
    }
    private static bool CompareLists(ListNode first, ListNode second)
    {
        ListNode? firstCurrent = first;
        ListNode? secondCurrent = second;
        
        while (firstCurrent is not null)
        {
            if (secondCurrent is null)
                return false;
            
            if (firstCurrent.Data != secondCurrent.Data)
                return false;
            
            firstCurrent = firstCurrent.Next;
            secondCurrent = secondCurrent.Next;
        }
        
        return secondCurrent is null;
    }
}