using Microsoft.Extensions.Caching.Memory;
using System.Runtime.InteropServices;

namespace SuggestionAppLibrary.DataAccess;


public class MongoSuggestionData : ISuggestionData
{
	private readonly IDbConnection _db;
	private readonly IUserData _userData;
	private readonly IMemoryCache _cache;
	private readonly IMongoCollection<SuggestionModel> _suggestions;
	private const string CacheName = "SuggestionData";

	public MongoSuggestionData(IDbConnection db, IUserData userData, IMemoryCache cache)
	{
		_db = db;
		_userData = userData;
		_cache = cache;
		_suggestions = _db.SuggestionCollection;
	}

	public async Task<List<SuggestionModel>> GetAllSuggestions()
	{
		var output = _cache.Get<List<SuggestionModel>>(CacheName);
		if (output == null)
		{
			var results = await _suggestions.FindAsync(s => s.Archived == false);
			output = results.ToList();

			_cache.Set(CacheName, output, TimeSpan.FromMinutes(1));
		}

		return output;
	}

	public async Task<List<SuggestionModel>> GetUsersSuggestions(string userId)
	{
		var output = _cache.Get<List<SuggestionModel>>(userId);

		if (output is null)
		{
			var results = await _suggestions.FindAsync(s => s.Author.Id == userId);
			output = results.ToList();

			_cache.Set(userId, output, TimeSpan.FromMinutes(1));
		}

		return output;
	}

	public async Task<List<SuggestionModel>> GetAllApprovedSuggestions()
	{
		var output = await GetAllSuggestions();
		return output.Where(x => x.ApprovedForRelease).ToList();
	}

	public async Task<SuggestionModel> GetSuggestion(string id)
	{
		var results = await _suggestions.FindAsync(s => s.Id == id);
		return results.FirstOrDefault();
	}

	public async Task<List<SuggestionModel>> GetAllSuggestionsWaitingForApproval()
	{
		var output = await GetAllSuggestions();
		return output.Where(x => x.ApprovedForRelease == false && x.Rejected == false).ToList();
	}

	public async Task UpdateSuggestion(SuggestionModel suggestion)
	{
		await _suggestions.ReplaceOneAsync(s => s.Id == suggestion.Id, suggestion);
		_cache.Remove(CacheName);
	}

	public async Task UpvoteSuggestion(string suggestionId, string userId)
	{
		var client = _db.Client;

		using var session = await client.StartSessionAsync(); //creates a transaction

		session.StartTransaction();

		try
		{
			var db = client.GetDatabase(_db.DbName);

			//update suggestion info
			var suggestionsInTransation = db.GetCollection<SuggestionModel>(_db.SuggestionCollectionName);
			var suggestion = (await suggestionsInTransation.FindAsync(s => s.Id == suggestionId)).First(); //uses first so we can throw an exception if we dont find the suggestion

			bool isUpvote = suggestion.UserVotes.Add(userId); //attempt to add item to hashset; returns true if successful, false if not
			if (isUpvote == false)
			{
				//clicked upvote again signalling removing the upvote
				suggestion.UserVotes.Remove(userId);
			}
			await suggestionsInTransation.ReplaceOneAsync(session, s => s.Id == suggestionId, suggestion);

			//update user info
			var usersInTransactions = db.GetCollection<UserModel>(_db.UserCollectionName);
			var user = await _userData.GetUser(userId);

			if (isUpvote)
			{
				user.VotedOnSuggestions.Add(new BasicSuggestionModel(suggestion));
			}
			else
			{
				var suggestionToRemove = user.VotedOnSuggestions.Where(s => s.Id == suggestionId).First();
				user.VotedOnSuggestions.Remove(new BasicSuggestionModel(suggestion));
			}
			await usersInTransactions.ReplaceOneAsync(session, u => u.Id == user.Id, user);

			await session.CommitTransactionAsync();

			_cache.Remove(CacheName);
		}
		catch (Exception ex)
		{
			await session.AbortTransactionAsync();
			throw;
		}
	}

	public async Task CreateSuggestion(SuggestionModel suggestion)
	{
		var client = _db.Client;
		using var session = await client.StartSessionAsync();
		session.StartTransaction();

		try
		{
			var db = client.GetDatabase(_db.DbName);
			var suggestionsInTransation = db.GetCollection<SuggestionModel>(_db.SuggestionCollectionName);
			await suggestionsInTransation.InsertOneAsync(suggestion);

			var usersInTransaction = db.GetCollection<UserModel>(_db.UserCollectionName);
			var user = await _userData.GetUser(suggestion.Author.Id);
			user.AuthoredSuggestions.Add(new BasicSuggestionModel(suggestion));
			await usersInTransaction.ReplaceOneAsync(u => u.Id == user.Id, user);

			await session.CommitTransactionAsync();
		}
		catch (Exception ex)
		{
			await session.AbortTransactionAsync();
			throw;
		}
	}
}


//https://youtu.be/J5xlZaiENKw?t=900