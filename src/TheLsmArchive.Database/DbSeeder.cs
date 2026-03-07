// using TheLsmArchive.Common.Constants;
// using TheLsmArchive.Database.DbContext;
// using TheLsmArchive.Database.DbContext.Abstractions;
// using TheLsmArchive.Database.Entities;
//
// namespace TheLsmArchive.Database;
//
// public static class DbSeeder
// {
//     public static void Seed(BaseDbContext context)
//     {
//         if (context.Shows.Any())
//         {
//             return;
//         }
//
//         context.Shows.AddRange(
//             new ShowEntity { Name = ShowName.DefiningDuke, },
//             new ShowEntity { Name = ShowName.Constellation, },
//             new ShowEntity { Name = ShowName.KnockBack, },
//             new ShowEntity { Name = ShowName.PunchingUp, },
//             new ShowEntity { Name = ShowName.QAndAsFiresideChatsAndPatreonExclusives, },
//             new ShowEntity { Name = ShowName.SacredSymbolsPlus, },
//             new ShowEntity { Name = ShowName.SacredSymbols, },
//             new ShowEntity { Name = ShowName.SummonSign, }
//         );
//
//         context.SaveChanges();
//     }
//
//     public static Task SeedAsync(BaseDbContext context)
//     {
//         return Task.Run(() => Seed(context));
//     }
// }
