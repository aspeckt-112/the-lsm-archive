using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Web.Api.Tests.TestSupport.Helpers;

internal static class PersonTestDataHelper
{
    internal static async Task<PersonTopicEntity> LinkTopicToPersonAsync(
        LsmArchiveDbContext dbContext,
        PersonEntity person,
        TopicEntity topic,
        CancellationToken cancellationToken)
    {
        PersonTopicEntity personTopic = new()
        {
            PersonId = person.Id,
            TopicId = topic.Id
        };

        await dbContext.PersonTopics.AddAsync(personTopic, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return personTopic;
    }
}

