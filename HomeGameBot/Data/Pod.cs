using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using X10D.Collections;

namespace HomeGameBot.Data;

internal sealed record Pod
{
    public int Id { get; set; }
    public ulong MessageId { get; set; }
    
    public List<User> Users { get; set; } = new();
    
    public string Location { get; set; }
    
    public string Type { get; set; }
    
    public int MaxPlayers { get; set; }
    
    public DateTime When { get; set; }
    
    [NotMapped]
    public ulong HostId => Users.First().UserId;
    [NotMapped]
    public User Host => Users.First();
    
    [NotMapped]
    public int CurrentPlayers => Users.Count;
    
    [NotMapped]
    public bool IsFull => CurrentPlayers >= MaxPlayers;
    [NotMapped]
    public bool HasExpired => DateTime.Now > When;
}