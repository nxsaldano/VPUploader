namespace VPUploader.Core.Models;

public class PinRequest
{
    public required string BoardId { get; set; }
    public required string ImagePath { get; set; }
    
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Link { get; set; }
    public string? AltText { get; set; }
    
}