namespace SuggestionAppLibrary.Models;


public class SuggestionModel
{
    [BsonId] //denotes an identifier
    [BsonRepresentation(BsonType.ObjectId)] //represents a unique identifier and should be assigned a value when we insert
    public string Id { get; set; }
    public string Suggestion { get; set; }
    public string Description { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public CategoryModel Category { get; set; }
    public BasicUserModel Author { get; set; }
    public HashSet<string> UserVotes { get; set; } = new();
    public StatusModel SuggestionStatus { get; set; }
    public string OwnerNotes { get; set; }

    public bool ApprovedForRelease { get; set; } = false;
    public bool Archived { get; set; } = false;
    public bool Rejected { get; set; } = false;

}
