﻿using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using GenericServices.Core;
using NUnit.Framework;
using Tests.DataClasses;
using Tests.DataClasses.Concrete;
using Tests.DTOs.Concrete;
using Tests.Helpers;

namespace Tests.UnitTests.Group09CrudServicesAsync
{
    class Test03CheckGenericDtoAsync
    {
        [TestFixtureSetUp]
        public void SetUpFixture()
        {
            using (var db = new SampleWebAppDb())
            {
                DataLayerInitialise.InitialiseThis();
                var filepath = TestFileHelpers.GetTestFileFilePath("DbContentSimple.xml");
                DataLayerInitialise.ResetDatabaseToTestData(db, filepath);
            }
        }

        [Test]
        public async void Check01CopyDtoToDataOk()
        {

            //SETUP
            var dto = new SimplePostDtoAsync
            {
                PostId = 123,
                BloggerName = "This should not be copied",
                Title = "Should copy this title",
                LastUpdated = new DateTime(2000, 1, 1),
                Tags = new Collection<Tag> { new Tag { Name = "Should not copy this", Slug = "No" } }
            };


            //ATTEMPT
            var newData = new Post
            {
                Blogger = new Blog { Name = "Original Blog Name" },
                BlogId = 777,
                Content = "Original Content",
                Tags = new Collection<Tag> { new Tag { Name = "Original Tag name", Slug = "Yes" } }
            };

            var status = await dto.CopyDtoToDataAsync(null, dto, newData);

            //VERIFY
            status.IsValid.ShouldEqual(true, status.Errors);
            newData.PostId.ShouldEqual(123);
            newData.Title.ShouldEqual("Should copy this title");

            newData.Blogger.Name.ShouldEqual("Original Blog Name");
            newData.BlogId.ShouldEqual(777);
            newData.Content.ShouldEqual("Original Content");
            newData.Tags.Count.ShouldEqual(1);
            newData.Tags.First().Name.ShouldEqual("Original Tag name");
        }

        [Test]
        public async void Check02CopyDtoToDataValidationFail()
        {

            //SETUP
            var dto = new SimplePostDtoAsync
            {
                PostId = 123,
                BloggerName = "This should not be copied",
                Title = null,
                LastUpdated = new DateTime(2000, 1, 1),
                Tags = new Collection<Tag> { new Tag { Name = "Should not copy this", Slug = "No" } }
            };


            //ATTEMPT
            var newData = new Post
            {
                Blogger = new Blog { Name = "Original Blog Name" },
                BlogId = 777,
                Content = "Original Content",
                Tags = new Collection<Tag> { new Tag { Name = "Original Tag name", Slug = "Yes" } }
            };

            var status = await dto.CopyDtoToDataAsync(null, dto, newData);

            //VERIFY
            status.IsValid.ShouldEqual(false, status.Errors);
            CollectionAssert.AreEquivalent(new[] { "The Title field is required." },
                status.Errors.Select(x => x.ErrorMessage));
        }


        [Test]
        public async void Check03CopyDtoToDataValidationFail()
        {

            //SETUP
            var dto = new SimpleTagDtoAsync();

            //ATTEMPT
            var newData = new Tag();

            var status = await dto.CopyDtoToDataAsync(null, dto, newData);

            //VERIFY
            status.IsValid.ShouldEqual(false, status.Errors);
            CollectionAssert.AreEquivalent(new[] { "The Slug field is required.", "The Name field is required." },
                status.Errors.Select(x => x.ErrorMessage));
        }

        [Test]
        public async void Check04CopyDtoToDataNoValidationOk()
        {

            //SETUP
            var dto = new SimpleTagDtoAsync();
            dto.SetSupportedFunctions(ServiceFunctions.DoNotValidateonCopyDtoToData);

            //ATTEMPT
            var newData = new Tag();

            var status = await dto.CopyDtoToDataAsync(null, dto, newData);

            //VERIFY
            status.IsValid.ShouldEqual(true, status.Errors);
        }

        //--------------------------

        [Test]
        public async void Check5CopyDataToDtoOk()
        {

            //SETUP
            var data = new Post
            {
                PostId = 666,
                Blogger = new Blog { Name = "Original Blog Name" },
                BlogId = 777,
                Title = "Original Title",
                Content = "Original Content",
                Tags = new Collection<Tag> { new Tag { Name = "Tag1", Slug = "one" }, new Tag { Name = "Tag2", Slug = "two" } }
            };

            //ATTEMPT
            var newDto = new SimplePostDtoAsync
            {
                PostId = 123,
                BloggerName = "this should be overwritten",
                Title = "this should be overwritten",
                LastUpdated = new DateTime(2000, 1, 1),
                Tags = new Collection<Tag> { new Tag { Name = "this should be overwritten", Slug = "No" } }
            };

            var status = await newDto.CopyDataToDtoAsync(null, data, newDto);

            //VERIFY
            status.IsValid.ShouldEqual(true, status.Errors);
            newDto.PostId.ShouldEqual(666);
            newDto.Title.ShouldEqual("Original Title");
            newDto.LastUpdated.ShouldEqual(data.LastUpdated);
            newDto.LastUpdatedUtc.Kind.ShouldEqual(DateTimeKind.Utc);

            newDto.BloggerName.ShouldEqual("Original Blog Name");
            newDto.Tags.Count.ShouldEqual(2);
            string.Join(",", newDto.Tags.Select(x => x.Slug)).ShouldEqual("one,two");
        }


        [Test]
        public async void Check06CreateDtoAndCopyInDataOk()
        {
            using (var db = new SampleWebAppDb())
            {
                //SETUP;
                var firstPost = db.Posts.Include( x => x.Blogger).Include(x => x.Tags).AsNoTracking().First();

                //ATTEMPT
                var newDto = await new SimplePostDtoAsync().CreateDtoAndCopyDataInAsync(db, x => x.PostId == firstPost.PostId);

                //VERIFY
                newDto.PostId.ShouldEqual(firstPost.PostId);
                newDto.Title.ShouldEqual(firstPost.Title);
                newDto.LastUpdated.ShouldEqual(firstPost.LastUpdated);
                newDto.LastUpdatedUtc.Kind.ShouldEqual(DateTimeKind.Utc);

                newDto.BloggerName.ShouldEqual(firstPost.Blogger.Name);
                CollectionAssert.AreEqual(firstPost.Tags.Select(x => x.TagId), newDto.Tags.Select(x => x.TagId));
            }
        }
    }
}
