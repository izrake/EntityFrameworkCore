﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.TestUtilities.Xunit;
using Xunit;

// ReSharper disable MergeConditionalExpression
// ReSharper disable ConstantNullCoalescingCondition
// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.Query
{
    public class QueryBugsInMemoryTest : IClassFixture<InMemoryFixture>
    {
        #region Bug9849

        [Fact]
        public void Include_throw_when_empty_9849()
        {
            using (CreateScratch<DatabaseContext>(_ => { }, "9849"))
            {
                using (var context = new DatabaseContext())
                {
                    var results = context.VehicleInspections.Include(_ => _.Motors).ToList();

                    Assert.Empty(results);
                }
            }
        }

        [Fact]
        public void Include_throw_when_empty_9849_2()
        {
            using (CreateScratch<DatabaseContext>(_ => { }, "9849"))
            {
                using (var context = new DatabaseContext())
                {
                    var results = context.VehicleInspections.Include(_foo => _foo.Motors).ToList();

                    Assert.Empty(results);
                }
            }
        }

        [Fact]
        public void Include_throw_when_empty_9849_3()
        {
            using (CreateScratch<DatabaseContext>(_ => { }, "9849"))
            {
                using (var context = new DatabaseContext())
                {
                    var results = context.VehicleInspections.Include(__ => __.Motors).ToList();

                    Assert.Empty(results);
                }
            }
        }

        [Fact]
        public void Include_throw_when_empty_9849_4()
        {
            using (CreateScratch<DatabaseContext>(_ => { }, "9849"))
            {
                using (var context = new DatabaseContext())
                {
                    var results = context.VehicleInspections.Include(___ => ___.Motors).ToList();

                    Assert.Empty(results);
                }
            }
        }

        [Fact]
        public void Include_throw_when_empty_9849_5()
        {
            using (CreateScratch<DatabaseContext>(_ => { }, "9849"))
            {
                using (var context = new DatabaseContext())
                {
                    var results
                        = (from _ in context.VehicleInspections
                           join _f in context.Motors on _.Id equals _f.Id
                           join __ in context.VehicleInspections on _f.Id equals __.Id
                           select _).ToList();

                    Assert.Empty(results);
                }
            }
        }

        [Fact]
        public void Include_throw_when_empty_9849_6()
        {
            using (CreateScratch<DatabaseContext>(_ => { }, "9849"))
            {
                using (var context = new DatabaseContext())
                {
                    var _ = 0L;
                    var __ = 0L;
                    var _f = 0L;

                    var results
                        = (from v in context.VehicleInspections
                           where v.Id == _ || v.Id == __ || v.Id == _f
                           select _).ToList();

                    Assert.Empty(results);
                }
            }
        }

        public class DatabaseContext : DbContext
        {
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseInMemoryDatabase("9849");
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                var builder = modelBuilder.Entity<VehicleInspection>();

                builder.HasMany(i => i.Motors).WithOne(a => a.Inspection).HasForeignKey(i => i.VehicleInspectionId);
            }

            public DbSet<VehicleInspection> VehicleInspections { get; set; }
            public DbSet<Motor> Motors { get; set; }
        }

        public class VehicleInspection
        {
            public long Id { get; set; }
            public ICollection<Motor> Motors { get; set; } = new HashSet<Motor>();
        }

        public class Motor
        {
            public long Id { get; set; }
            public long VehicleInspectionId { get; set; }
            public VehicleInspection Inspection { get; set; }
        }

        #endregion

        #region Bug3595

        [Fact]
        public void GroupBy_with_uninitialized_datetime_projection_3595()
        {
            using (CreateScratch<Context3595>(Seed3595, "3595"))
            {
                using (var context = new Context3595())
                {
                    var q0 = from instance in context.Exams
                             join question in context.ExamQuestions
                                 on instance.Id equals question.ExamId
                             where instance.Id != 3
                             group question by question.QuestionId
                             into gQuestions
                             select new
                             {
                                 gQuestions.Key,
                                 MaxDate = gQuestions.Max(q => q.Modified)
                             };

                    var result = q0.ToList();

                    Assert.Equal(default, result.Single().MaxDate);
                }
            }
        }

        private static void Seed3595(Context3595 context)
        {
            var question = new Question3595();
            var examInstance = new Exam3595();
            var examInstanceQuestion = new ExamQuestion3595
            {
                Question = question,
                Exam = examInstance
            };

            context.Add(question);
            context.Add(examInstance);
            context.Add(examInstanceQuestion);
            context.SaveChanges();
        }

        public abstract class Base3595
        {
            public DateTime Modified { get; set; }
        }

        public class Question3595 : Base3595
        {
            public int Id { get; set; }
        }

        public class Exam3595 : Base3595
        {
            public int Id { get; set; }
        }

        public class ExamQuestion3595 : Base3595
        {
            public int Id { get; set; }

            public int QuestionId { get; set; }
            public Question3595 Question { get; set; }

            public int ExamId { get; set; }
            public Exam3595 Exam { get; set; }
        }

        public class Context3595 : DbContext
        {
            public DbSet<Exam3595> Exams { get; set; }
            public DbSet<Question3595> Questions { get; set; }
            public DbSet<ExamQuestion3595> ExamQuestions { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseInMemoryDatabase("3595");
            }
        }

        #endregion

        #region Bug3101

        [Fact]
        public virtual void Repro3101_simple_coalesce1()
        {
            using (CreateScratch<MyContext3101>(Seed3101, "3101"))
            {
                using (var ctx = new MyContext3101())
                {
                    var query = from eVersion in ctx.Entities
                                join eRoot in ctx.Entities.Include(e => e.Children).AsNoTracking()
                                    on eVersion.RootEntityId equals (int?)eRoot.Id
                                    into RootEntities
                                from eRootJoined in RootEntities.DefaultIfEmpty()
                                select eRootJoined ?? eVersion;

                    Assert.Equal(3, query.ToList().Count);
                }
            }
        }

        [Fact]
        public virtual void Repro3101_simple_coalesce2()
        {
            using (CreateScratch<MyContext3101>(Seed3101, "3101"))
            {
                using (var ctx = new MyContext3101())
                {
                    var query = from eVersion in ctx.Entities
                                join eRoot in ctx.Entities.Include(e => e.Children)
                                    on eVersion.RootEntityId equals (int?)eRoot.Id
                                    into RootEntities
                                from eRootJoined in RootEntities.DefaultIfEmpty()
                                select eRootJoined ?? eVersion;

                    var result = query.ToList();
                    Assert.Equal(2, result.Count(e => e.Children.Count > 0));
                }
            }
        }

        [Fact]
        public virtual void Repro3101_simple_coalesce3()
        {
            using (CreateScratch<MyContext3101>(Seed3101, "3101"))
            {
                using (var ctx = new MyContext3101())
                {
                    var query = from eVersion in ctx.Entities.Include(e => e.Children)
                                join eRoot in ctx.Entities.Include(e => e.Children)
                                    on eVersion.RootEntityId equals (int?)eRoot.Id
                                    into RootEntities
                                from eRootJoined in RootEntities.DefaultIfEmpty()
                                select eRootJoined ?? eVersion;

                    var result = query.ToList();

                    Assert.True(result.All(e => e.Children.Count > 0));
                }
            }
        }

        [Fact]
        public virtual void Repro3101_complex_coalesce1()
        {
            using (CreateScratch<MyContext3101>(Seed3101, "3101"))
            {
                using (var ctx = new MyContext3101())
                {
                    var query = from eVersion in ctx.Entities.Include(e => e.Children)
                                join eRoot in ctx.Entities
                                    on eVersion.RootEntityId equals (int?)eRoot.Id
                                    into RootEntities
                                from eRootJoined in RootEntities.DefaultIfEmpty()
                                select new
                                {
                                    One = 1,
                                    Coalesce = eRootJoined ?? eVersion
                                };

                    var result = query.ToList();
                    Assert.True(result.All(e => e.Coalesce.Children.Count > 0));
                }
            }
        }

        [Fact]
        public virtual void Repro3101_complex_coalesce2()
        {
            using (CreateScratch<MyContext3101>(Seed3101, "3101"))
            {
                using (var ctx = new MyContext3101())
                {
                    var query = from eVersion in ctx.Entities
                                join eRoot in ctx.Entities.Include(e => e.Children)
                                    on eVersion.RootEntityId equals (int?)eRoot.Id
                                    into RootEntities
                                from eRootJoined in RootEntities.DefaultIfEmpty()
                                select new
                                {
                                    Root = eRootJoined,
                                    Coalesce = eRootJoined ?? eVersion
                                };

                    var result = query.ToList();
                    Assert.Equal(2, result.Count(e => e.Coalesce.Children.Count > 0));
                }
            }
        }

        [Fact]
        public virtual void Repro3101_nested_coalesce1()
        {
            using (CreateScratch<MyContext3101>(Seed3101, "3101"))
            {
                using (var ctx = new MyContext3101())
                {
                    var query = from eVersion in ctx.Entities
                                join eRoot in ctx.Entities.Include(e => e.Children)
                                    on eVersion.RootEntityId equals (int?)eRoot.Id
                                    into RootEntities
                                from eRootJoined in RootEntities.DefaultIfEmpty()
                                select new
                                {
                                    One = 1,
                                    Coalesce = eRootJoined ?? (eVersion ?? eRootJoined)
                                };

                    var result = query.ToList();
                    Assert.Equal(2, result.Count(e => e.Coalesce.Children.Count > 0));
                }
            }
        }

        [Fact]
        public virtual void Repro3101_nested_coalesce2()
        {
            using (CreateScratch<MyContext3101>(Seed3101, "3101"))
            {
                using (var ctx = new MyContext3101())
                {
                    var query = from eVersion in ctx.Entities.Include(e => e.Children)
                                join eRoot in ctx.Entities
                                    on eVersion.RootEntityId equals (int?)eRoot.Id
                                    into RootEntities
                                from eRootJoined in RootEntities.DefaultIfEmpty()
                                select new
                                {
                                    One = eRootJoined,
                                    Two = 2,
                                    Coalesce = eRootJoined ?? (eVersion ?? eRootJoined)
                                };

                    var result = query.ToList();
                    Assert.True(result.All(e => e.Coalesce.Children.Count > 0));
                }
            }
        }

        [Fact]
        public virtual void Repro3101_conditional()
        {
            using (CreateScratch<MyContext3101>(Seed3101, "3101"))
            {
                using (var ctx = new MyContext3101())
                {
                    var query = from eVersion in ctx.Entities.Include(e => e.Children)
                                join eRoot in ctx.Entities
                                    on eVersion.RootEntityId equals (int?)eRoot.Id
                                    into RootEntities
                                from eRootJoined in RootEntities.DefaultIfEmpty()
#pragma warning disable IDE0029 // Use coalesce expression
                                select eRootJoined != null ? eRootJoined : eVersion;
#pragma warning restore IDE0029 // Use coalesce expression

                    var result = query.ToList();
                    Assert.True(result.All(e => e.Children.Count > 0));
                }
            }
        }

        [Fact]
        public virtual void Repro3101_coalesce_tracking()
        {
            using (CreateScratch<MyContext3101>(Seed3101, "3101"))
            {
                using (var ctx = new MyContext3101())
                {
                    var query = from eVersion in ctx.Entities
                                join eRoot in ctx.Entities
                                    on eVersion.RootEntityId equals (int?)eRoot.Id
                                    into RootEntities
                                from eRootJoined in RootEntities.DefaultIfEmpty()
                                select new
                                {
                                    eRootJoined,
                                    eVersion,
                                    foo = eRootJoined ?? eVersion
                                };

                    Assert.Equal(3, query.ToList().Count);

                    Assert.True(ctx.ChangeTracker.Entries().Any());
                }
            }
        }

        private static void Seed3101(MyContext3101 context)
        {
            var c11 = new Child3101
            {
                Name = "c11"
            };
            var c12 = new Child3101
            {
                Name = "c12"
            };
            var c13 = new Child3101
            {
                Name = "c13"
            };
            var c21 = new Child3101
            {
                Name = "c21"
            };
            var c22 = new Child3101
            {
                Name = "c22"
            };
            var c31 = new Child3101
            {
                Name = "c31"
            };
            var c32 = new Child3101
            {
                Name = "c32"
            };

            context.Children.AddRange(c11, c12, c13, c21, c22, c31, c32);

            var e1 = new Entity3101
            {
                Id = 1,
                Children = new[] { c11, c12, c13 }
            };
            var e2 = new Entity3101
            {
                Id = 2,
                Children = new[] { c21, c22 }
            };
            var e3 = new Entity3101
            {
                Id = 3,
                Children = new[] { c31, c32 }
            };

            e2.RootEntity = e1;

            context.Entities.AddRange(e1, e2, e3);
            context.SaveChanges();
        }

        public class MyContext3101 : DbContext
        {
            public DbSet<Entity3101> Entities { get; set; }

            public DbSet<Child3101> Children { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseInMemoryDatabase("3101");
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Entity3101>().Property(e => e.Id).ValueGeneratedNever();
            }
        }

        public class Entity3101
        {
            public Entity3101()
            {
                Children = new Collection<Child3101>();
            }

            public int Id { get; set; }

            public int? RootEntityId { get; set; }

            public Entity3101 RootEntity { get; set; }

            public ICollection<Child3101> Children { get; set; }
        }

        public class Child3101
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        #endregion

        #region Bug5456

        [Fact]
        public virtual void Repro5456_include_group_join_is_per_query_context()
        {
            using (CreateScratch<MyContext5456>(Seed5456, "5456"))
            {
                Parallel.For(
                    0, 10, i =>
                    {
                        using (var ctx = new MyContext5456())
                        {
                            var result = ctx.Posts.Where(x => x.Blog.Id > 1).Include(x => x.Blog).ToList();

                            Assert.Equal(198, result.Count);
                        }
                    });
            }
        }

        [Fact]
        public virtual void Repro5456_include_group_join_is_per_query_context_async()
        {
            using (CreateScratch<MyContext5456>(Seed5456, "5456"))
            {
                Parallel.For(
                    0, 10, async i =>
                    {
                        using (var ctx = new MyContext5456())
                        {
                            var result = await ctx.Posts.Where(x => x.Blog.Id > 1).Include(x => x.Blog).ToListAsync();

                            Assert.Equal(198, result.Count);
                        }
                    });
            }
        }

        [Fact]
        public virtual void Repro5456_multiple_include_group_join_is_per_query_context()
        {
            using (CreateScratch<MyContext5456>(Seed5456, "5456"))
            {
                Parallel.For(
                    0, 10, i =>
                    {
                        using (var ctx = new MyContext5456())
                        {
                            var result = ctx.Posts.Where(x => x.Blog.Id > 1).Include(x => x.Blog).Include(x => x.Comments).ToList();

                            Assert.Equal(198, result.Count);
                        }
                    });
            }
        }

        [Fact]
        public virtual void Repro5456_multiple_include_group_join_is_per_query_context_async()
        {
            using (CreateScratch<MyContext5456>(Seed5456, "5456"))
            {
                Parallel.For(
                    0, 10, async i =>
                    {
                        using (var ctx = new MyContext5456())
                        {
                            var result = await ctx.Posts.Where(x => x.Blog.Id > 1).Include(x => x.Blog).Include(x => x.Comments).ToListAsync();

                            Assert.Equal(198, result.Count);
                        }
                    });
            }
        }

        [Fact]
        public virtual void Repro5456_multi_level_include_group_join_is_per_query_context()
        {
            using (CreateScratch<MyContext5456>(Seed5456, "5456"))
            {
                Parallel.For(
                    0, 10, i =>
                    {
                        using (var ctx = new MyContext5456())
                        {
                            var result = ctx.Posts.Where(x => x.Blog.Id > 1).Include(x => x.Blog).ThenInclude(b => b.Author).ToList();

                            Assert.Equal(198, result.Count);
                        }
                    });
            }
        }

        [Fact]
        public virtual void Repro5456_multi_level_include_group_join_is_per_query_context_async()
        {
            using (CreateScratch<MyContext5456>(Seed5456, "5456"))
            {
                Parallel.For(
                    0, 10, async i =>
                    {
                        using (var ctx = new MyContext5456())
                        {
                            var result = await ctx.Posts.Where(x => x.Blog.Id > 1).Include(x => x.Blog).ThenInclude(b => b.Author).ToListAsync();

                            Assert.Equal(198, result.Count);
                        }
                    });
            }
        }

        private void Seed5456(MyContext5456 context)
        {
            for (var i = 0; i < 100; i++)
            {
                context.Add(
                    new Blog5456
                    {
                        Id = i + 1,
                        Posts = new List<Post5456>
                        {
                            new Post5456
                            {
                                Comments = new List<Comment5456>
                                {
                                    new Comment5456(),
                                    new Comment5456()
                                }
                            },
                            new Post5456()
                        },
                        Author = new Author5456()
                    });
            }

            context.SaveChanges();
        }

        public class MyContext5456 : DbContext
        {
            public DbSet<Blog5456> Blogs { get; set; }
            public DbSet<Post5456> Posts { get; set; }
            public DbSet<Comment5456> Comments { get; set; }
            public DbSet<Author5456> Authors { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseInMemoryDatabase("5456");
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Blog5456>().Property(e => e.Id).ValueGeneratedNever();
            }
        }

        public class Blog5456
        {
            public int Id { get; set; }
            public List<Post5456> Posts { get; set; }
            public Author5456 Author { get; set; }
        }

        public class Author5456
        {
            public int Id { get; set; }
            public List<Blog5456> Blogs { get; set; }
        }

        public class Post5456
        {
            public int Id { get; set; }
            public Blog5456 Blog { get; set; }
            public List<Comment5456> Comments { get; set; }
        }

        public class Comment5456
        {
            public int Id { get; set; }
            public Post5456 Blog { get; set; }
        }

        #endregion

        #region Bug8282

        [Fact]
        public virtual void Entity_passed_to_DTO_constructor_works()
        {
            using (CreateScratch<MyContext8282>(e => { }, "8282"))
            {
                using (var context = new MyContext8282())
                {
                    var query = context.Entity.Select(e => new EntityDto8282(e)).ToList();

                    Assert.Equal(0, query.Count);
                }
            }
        }

        private class MyContext8282 : DbContext
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public DbSet<Entity8282> Entity { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseInMemoryDatabase("8282");
            }
        }

        public class Entity8282
        {
            public int Id { get; set; }
        }

        public class EntityDto8282
        {
            public EntityDto8282(Entity8282 entity)
            {
                Id = entity.Id;
            }

            public int Id { get; set; }
        }

        #endregion

        #region Bug13108

        [Fact]
        public virtual void HasForeignKey_infers_type_for_shadow_property_when_not_specified()
        {
            using (CreateScratch<MyContext13108>(e => { }, "13108"))
            {
                using (var context = new MyContext13108())
                {
                    var model = (Model)context.Model;
                    Assert.Equal(ConfigurationSource.Convention,
                        model.FindEntityType(typeof(ComplexCaseChild13108))
                        .GetProperties().Where(p => p.Name == "ParentKey").Single()
                        .GetTypeConfigurationSource());
                }
            }
        }

        private class MyContext13108 : DbContext
        {
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseInMemoryDatabase("13108");
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<ComplexCaseChild13108>(b =>
                {
                    b.HasKey(c => c.Key);
                    b.Property("ParentKey");
                    b.HasOne(c => c.Parent).WithMany(c => c.Children).HasForeignKey("ParentKey");
                });

                modelBuilder.Entity<ComplexCaseParent13108>().HasKey(c => c.Key);
            }
        }

        public class ComplexCaseChild13108
        {
            public int Key { get; set; }
            public string Id { get; set; }
            private int ParentKey { get; set; }
            public ComplexCaseParent13108 Parent { get; set; }
        }

        public class ComplexCaseParent13108
        {
            public int Key { get; set; }
            public string Id { get; set; }
            public ICollection<ComplexCaseChild13108> Children { get; set; }
        }

        #endregion

        #region Bug12617

        [Fact]
        [UseCulture("de-DE")]
        public virtual void EntityType_name_is_stored_culture_invariantly()
        {
            using (CreateScratch<MyContext12617>(e => { }, "12617"))
            {
                using (var context = new MyContext12617())
                {
                    Assert.Equal(2, context.Model.GetEntityTypes().Count());
                    Assert.Equal(2, context.Model.FindEntityType(typeof(Entityss)).GetNavigations().Count());
                }
            }
        }

        private class MyContext12617 : DbContext
        {
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseInMemoryDatabase("12617");
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Entityß>();
                modelBuilder.Entity<Entityss>();
            }
        }

        public class Entityß
        {
            public int Id { get; set; }
        }

        public class Entityss
        {
            public int Id { get; set; }
            public Entityß Navigationß { get; set; }
            public Entityß Navigationss { get; set; }
        }

        #endregion

        #region Bug13300

        [Fact]
        public virtual void Explicitly_set_shadow_FK_name_is_preserved_with_HasPrincipalKey()
        {
            using (CreateScratch<MyContext13300>(e => { }, "13300"))
            {
                using (var context = new MyContext13300())
                {
                    var model = context.Model;

                    var fk = model.FindEntityType(typeof(Profile13300)).GetForeignKeys().Single();
                    Assert.Equal("_profiles", fk.PrincipalToDependent.Name);
                    Assert.Equal("User", fk.DependentToPrincipal.Name);
                    Assert.Equal("Email", fk.Properties[0].Name);
                    Assert.Equal(typeof(string), fk.Properties[0].ClrType);
                    Assert.Equal("_email", fk.PrincipalKey.Properties[0].Name);
                }
            }
        }

        private class MyContext13300 : DbContext
        {
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseInMemoryDatabase("13300");
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<User13300>(m =>
                {
                    m.Property("_email");

                    m.HasMany<Profile13300>("_profiles")
                        .WithOne("User")
                        .HasForeignKey("Email")
                        .HasPrincipalKey("_email");
                });

                modelBuilder.Entity<Profile13300>(m =>
                {
                    m.Property<string>("Email");
                });
            }
        }

        public class User13300
        {
            public Guid Id { get; set; }
            private string _email = string.Empty;
            private readonly List<Profile13300> _profiles = new List<Profile13300>();
        }

        public class Profile13300
        {
            public Guid Id { get; set; }
            public User13300 User { get; set; }
        }

        #endregion

        #region Bug13694

        [Fact]
        public virtual void Attribute_set_shadow_FK_name_is_preserved_with_HasPrincipalKey()
        {
            using (CreateScratch<MyContext13694>(e => { }, "13694"))
            {
                using (var context = new MyContext13694())
                {
                    var model = context.Model;

                    var fk = model.FindEntityType(typeof(Profile13694)).GetForeignKeys().Single();
                    Assert.Equal("_profiles", fk.PrincipalToDependent.Name);
                    Assert.Equal("User", fk.DependentToPrincipal.Name);
                    Assert.Equal("Email", fk.Properties[0].Name);
                    Assert.Equal(typeof(string), fk.Properties[0].ClrType);
                    Assert.Equal("_email", fk.PrincipalKey.Properties[0].Name);
                }
            }
        }

        private class MyContext13694 : DbContext
        {
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseInMemoryDatabase("13694");
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<User13694>(m =>
                {
                    m.Property("_email");

                    m.HasMany<Profile13694>("_profiles")
                        .WithOne("User")
                        .HasPrincipalKey("_email");
                });

                modelBuilder.Entity<Profile13694>(m =>
                {
                    m.Property<string>("Email");
                });
            }
        }

        public class User13694
        {
            public Guid Id { get; set; }
            private string _email = string.Empty;
            private readonly List<Profile13694> _profiles = new List<Profile13694>();
        }

        public class Profile13694
        {
            public Guid Id { get; set; }
            [ForeignKey("Email")]
            public User13694 User { get; set; }
        }

        #endregion

        #region SharedHelper

        private static InMemoryTestStore CreateScratch<TContext>(Action<TContext> seed, string databaseName)
            where TContext : DbContext, new()
        {
            return InMemoryTestStore.GetOrCreate(databaseName)
                .InitializeInMemory(null, () => new TContext(), c => seed((TContext)c));
        }

        #endregion
    }
}
