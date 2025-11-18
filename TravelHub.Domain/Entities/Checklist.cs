namespace TravelHub.Domain.Entities;

//public class Checklist
//{
//    public Dictionary<string, bool> Items { get; set; } = new();

//    public void AddItem(string item, bool isCompleted = false)
//    {
//        Items[item] = isCompleted;
//    }

//    public void MarkComplete(string item)
//    {
//        if (Items.ContainsKey(item))
//            Items[item] = true;
//    }

//    public int CompletedCount => Items.Count(x => x.Value);
//    public int TotalCount => Items.Count;
//}

public class Checklist
{
    public List<ChecklistItem> Items { get; set; } = new();

    public void AddItem(string title, bool isCompleted = false)
    {
        if (Items.Any(i => i.Title == title))
            return;
        Items.Add(new ChecklistItem { Title = title, IsCompleted = isCompleted });
    }

    public void RemoveItem(string title)
    {
        var it = Items.FirstOrDefault(i => i.Title == title);
        if (it != null) Items.Remove(it);
    }

    public ChecklistItem? Find(string title) => Items.FirstOrDefault(i => i.Title == title);

    public int CompletedCount => Items.Count(x => x.IsCompleted);
    public int TotalCount => Items.Count;
}

public class ChecklistItem
{
    public string Title { get; set; } = "";

    public bool IsCompleted { get; set; } = false;

    public string? AssignedParticipantId { get; set; }

    public string? AssignedParticipantName { get; set; }
}
