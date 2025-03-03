﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

public abstract class ManyToManyHeterogeneousQueryTestBase : NonSharedModelTestBase
{
    public static IEnumerable<object[]> IsAsyncData = new[] { new object[] { false }, new object[] { true } };

    protected override string StoreName
        => "ManyToManyHeterogeneousQueryTests";

    protected virtual void ClearLog()
        => ListLoggerFactory.Clear();

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Many_to_many_load_works_when_join_entity_has_custom_key(bool async)
    {
        var contextFactory = await InitializeAsync<Context20277>();

        int id;
        using (var context = contextFactory.CreateContext())
        {
            var m = new ManyM_DB();
            var n = new ManyN_DB();
            context.AddRange(m, n);
            m.ManyN_DB = new List<ManyN_DB> { n };

            context.SaveChanges();

            id = m.Id;
        }

        ClearLog();

        using (var context = contextFactory.CreateContext())
        {
            var m = context.Find<ManyM_DB>(id);

            if (async)
            {
                await context.Entry(m).Collection(x => x.ManyN_DB).LoadAsync();
            }
            else
            {
                context.Entry(m).Collection(x => x.ManyN_DB).Load();
            }

            Assert.Equal(3, context.ChangeTracker.Entries().Count());
            Assert.Equal(1, m.ManyN_DB.Count);
            Assert.Equal(1, m.ManyN_DB.Single().ManyM_DB.Count);
            Assert.Equal(1, m.ManyNM_DB.Count);
            Assert.Equal(1, m.ManyN_DB.Single().ManyNM_DB.Count);

            id = m.ManyN_DB.Single().Id;
        }

        using (var context = contextFactory.CreateContext())
        {
            var n = context.Find<ManyN_DB>(id);

            if (async)
            {
                await context.Entry(n).Collection(x => x.ManyM_DB).LoadAsync();
            }
            else
            {
                context.Entry(n).Collection(x => x.ManyM_DB).Load();
            }

            Assert.Equal(3, context.ChangeTracker.Entries().Count());
            Assert.Equal(1, n.ManyM_DB.Count);
            Assert.Equal(1, n.ManyM_DB.Single().ManyN_DB.Count);
            Assert.Equal(1, n.ManyNM_DB.Count);
            Assert.Equal(1, n.ManyM_DB.Single().ManyNM_DB.Count);
        }
    }

    protected class Context20277 : DbContext
    {
        public Context20277(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<ManyM_DB>()
                .HasMany(e => e.ManyN_DB)
                .WithMany(e => e.ManyM_DB)
                .UsingEntity<ManyMN_DB>(
                    r => r.HasOne(e => e.ManyN_DB).WithMany(e => e.ManyNM_DB).HasForeignKey(e => e.ManyN_Id),
                    l => l.HasOne(e => e.ManyM_DB).WithMany(e => e.ManyNM_DB).HasForeignKey(e => e.ManyM_Id),
                    b => b.HasKey(e => e.Id));
    }

    public class ManyM_DB
    {
        public int Id { get; set; }
        public ICollection<ManyN_DB> ManyN_DB { get; set; }
        public ICollection<ManyMN_DB> ManyNM_DB { get; set; }
    }

    public class ManyN_DB
    {
        public int Id { get; set; }
        public ICollection<ManyM_DB> ManyM_DB { get; set; }
        public ICollection<ManyMN_DB> ManyNM_DB { get; set; }
    }

    public sealed class ManyMN_DB
    {
        public int Id { get; set; }

        public int ManyM_Id { get; set; }
        public ManyM_DB ManyM_DB { get; set; }

        public int? ManyN_Id { get; set; }
        public ManyN_DB ManyN_DB { get; set; }
    }
}
