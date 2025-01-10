namespace HomeGameBot.Data;

internal sealed record User
{
    public int Id { get; set; }
    public ulong UserId { get; set; }
    public string DisplayName { get; set; }
    public List<Pod> Pods { get; set; } = new();
    
    public bool IsInPod => Pods.Count != 0;
}