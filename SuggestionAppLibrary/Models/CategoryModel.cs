namespace SuggestionAppLibrary.Models;

public class CategoryModel
{
    //bson is similar to json and is a bin­ary-en­coded seri­al­iz­a­tion of JSON documents.
    //see mongodb doc here https://www.mongodb.com/basics/bson

    [BsonId] //denotes an identifier
    [BsonRepresentation(BsonType.ObjectId)] //represents a unique identifier and should be assigned a value when we insert
    public string Id { get; set; }
    public string CategoryName { get; set; }
    public string CategoryDescription { get; set; }

}
