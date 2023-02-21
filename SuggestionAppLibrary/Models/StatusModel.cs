namespace SuggestionAppLibrary.Models;


public class StatusModel
{
    [BsonId] //denotes an identifier
    [BsonRepresentation(BsonType.ObjectId)] //represents a unique identifier and should be assigned a value when we insert
    public string Id { get; set; }
    public string StatusName { get; set; }
    public string StatusDescription { get; set; }
}
