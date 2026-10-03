using System.Text.Json.Serialization;

namespace VPUploader.Core.Models;
    
public class Board
    {
        
    [JsonPropertyName("id")] 
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] 
    public string Name { get; set; } = string.Empty;
    public override string ToString() => Name;
    
    public class BoardListResponse
        {
            [JsonPropertyName("items")]
            public List<Board> Items { get; set; } = new();

            [JsonPropertyName("bookmark")]
            public string? Bookmark { get; set; }
    }
    
    }

