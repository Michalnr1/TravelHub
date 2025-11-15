namespace TravelHub.Domain.Entities;

public class Checklist
{
    public Dictionary<string, bool> Items { get; set; } = new();

    public void AddItem(string item, bool isCompleted = false)
    {
        Items[item] = isCompleted;
    }

    public void MarkComplete(string item)
    {
        if (Items.ContainsKey(item))
            Items[item] = true;
    }

    public int CompletedCount => Items.Count(x => x.Value);
    public int TotalCount => Items.Count;
}
