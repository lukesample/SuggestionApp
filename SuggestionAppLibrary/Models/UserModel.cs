namespace SuggestionAppLibrary.Models;


public class UserModel
{
    [BsonId] //denotes an identifier
    [BsonRepresentation(BsonType.ObjectId)] //represents a unique identifier and should be assigned a value when we insert
    public string Id { get; set; }
    public string ObjectIdntifier { get; set; } //from Azure Active Directory B2C
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DisplayName { get; set; }
    public string EmailAddress { get; set; }
    public List<BasicSuggestionModel> AuthoredSuggestions { get; set; } = new();
    public List<BasicSuggestionModel> VotedOnSuggestions { get; set; } = new();
}
