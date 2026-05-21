using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Web.Api.Tests.TestSupport.Helpers;

internal static class SystemTestDataHelper
{
	internal static async Task<ShowEntity> CreateShowAsync(
		LsmArchiveDbContext dbContext,
		CancellationToken cancellationToken,
		string showName = "System Test Show")
	{
		ShowEntity show = new() { Name = showName };
		await dbContext.Shows.AddAsync(show, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);
		return show;
	}

	internal static async Task CreateProcessedPatreonPostAsync(
		LsmArchiveDbContext dbContext,
		ShowEntity show,
		DateTimeOffset published,
		int patreonId,
		CancellationToken cancellationToken,
		string titlePrefix = "System Test Post")
	{
		PatreonPostEntity patreonPost = await CreateUnprocessedPatreonPostAsync(
			dbContext,
			show,
			published,
			patreonId,
			cancellationToken,
			titlePrefix);

		EpisodeEntity episode = new()
		{
			ShowId = show.Id,
			Title = patreonPost.Title,
			ReleaseDateUtc = published,
			PatreonPostId = patreonPost.Id
		};

		await dbContext.Episodes.AddAsync(episode, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		patreonPost.EpisodeId = episode.Id;
		await dbContext.SaveChangesAsync(cancellationToken);
	}

	internal static async Task<PatreonPostEntity> CreateUnprocessedPatreonPostAsync(
		LsmArchiveDbContext dbContext,
		ShowEntity show,
		DateTimeOffset published,
		int patreonId,
		CancellationToken cancellationToken,
		string titlePrefix = "System Test Post")
	{
		PatreonPostEntity patreonPost = new()
		{
			ShowId = show.Id,
			PatreonId = patreonId,
			Title = $"{titlePrefix} {patreonId}",
			Published = published,
			Summary = $"Summary for post {patreonId}",
			Link = $"https://www.patreon.com/posts/{patreonId}",
			AudioUrl = $"https://www.patreon.com/posts/{patreonId}/audio"
		};

		await dbContext.PatreonPosts.AddAsync(patreonPost, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		return patreonPost;
	}
}


