﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoBondo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Ploeh.AutoFixture;
using Shouldly;

namespace IntegrationTests
{
    /// <summary>
    /// MongoDB is now started and stopped from inside the tests! Now need to have it running beforehand.
    /// </summary>
    [TestClass]
    public class IntegrationTest
    {
        private static Process _mongodProcess;

        private Fixture _fixture;
        private IMongoHelper _mongoHelper;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            _mongodProcess = new Process { StartInfo = { FileName = "mongod", Arguments = "--smallfiles --port 27027" } }; // Use non-standard port
            _mongodProcess.Start(); 
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            _mongodProcess.CloseMainWindow();
        }

        [TestInitialize]
        public void Setup()
        {
            _fixture = new Fixture();

            // Create a new database with each test to ensure test isolation
            _mongoHelper = new MongoHelper("mongodb://127.0.0.1:27027", "monghost-integration-tests-" + Guid.NewGuid());
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _mongoHelper.Database.Drop();
        }

        [TestMethod]
        public void SaveShouldPopulateId()
        {
            var entityFromFixture = _fixture.Build<EntityWithObjectId>().Without(x => x.Id).Create();
            _mongoHelper.Save(entityFromFixture);

            entityFromFixture.Id.ShouldNotBe(ObjectId.Empty);
        }

        [TestMethod]
        public void ShouldPluralizeAndLowercaseCollectionName()
        {
            var entityFromFixture = _fixture.Build<EntityWithObjectId>().Without(x => x.Id).Create();
            _mongoHelper.Save(entityFromFixture);

            var entitytFromDb = _mongoHelper.Database.GetCollection<EntityWithObjectId>("entitywithobjectids").FindOne();
            entitytFromDb.ShouldNotBe(null);
        }

        [TestMethod]
        public void ShouldSaveAndFind()
        {
            var entityFromFixture = _fixture.Build<EntityWithObjectId>().Without(x => x.Id).Create();
            _mongoHelper.Save(entityFromFixture);

            var entitytFromDb = _mongoHelper.FindOne<EntityWithObjectId>(entityFromFixture.Id);

            entitytFromDb.Id.ShouldBe(entityFromFixture.Id);
            entitytFromDb.String.ShouldBe(entityFromFixture.String);
            entitytFromDb.Bool.ShouldBe(entityFromFixture.Bool);
            entitytFromDb.List.Count.ShouldBe(entityFromFixture.List.Count);
        }

        [TestMethod]
        public void ShouldRemove()
        {
            var entityFromFixture = _fixture.Build<EntityWithObjectId>().Without(x => x.Id).Create();
            _mongoHelper.Save(entityFromFixture);

            _mongoHelper.Remove(entityFromFixture);

            var entitytFromDb = _mongoHelper.FindOne<EntityWithObjectId>(entityFromFixture.Id);

            entitytFromDb.ShouldBe(null);
        }

        [TestMethod]
        public void ShouldRemoveWithNonObjectIdId()
        {
            var entityFromFixture = _fixture.Build<EntityWithNonObjectIdId>().Create();
            _mongoHelper.Save(entityFromFixture);

            _mongoHelper.Remove<EntityWithNonObjectIdId>(entityFromFixture.Id);

            var entitytFromDb = _mongoHelper.FindOne<EntityWithNonObjectIdId>(entityFromFixture.Id);

            entitytFromDb.ShouldBe(null);
        }

        [TestMethod]
        public void ShouldRemoveWithNoId()
        {
            var entityFromFixture = _fixture.Build<EntityWithNonId>().Create();
            _mongoHelper.Save(entityFromFixture);

            _mongoHelper.Remove<EntityWithNonId>(entityFromFixture.Name);

            var entitytFromDb = _mongoHelper.FindOne<EntityWithNonId>(entityFromFixture.Name);

            entitytFromDb.ShouldBe(null);
        }

        [TestMethod]
        public void ShouldSaveAndFindObjectWithNonObjectIdId()
        {
            var entityFromFixture = _fixture.Build<EntityWithNonObjectIdId>().Create();
            _mongoHelper.Save(entityFromFixture);

            var entitytFromDb = _mongoHelper.FindOne<EntityWithNonObjectIdId>(entityFromFixture.Id);

            entitytFromDb.Id.ShouldBe(entityFromFixture.Id);
            entitytFromDb.String.ShouldBe(entityFromFixture.String);
            entitytFromDb.Bool.ShouldBe(entityFromFixture.Bool);
            entitytFromDb.List.Count.ShouldBe(entityFromFixture.List.Count);
        }

        [TestMethod]
        public void ShouldSaveAndFindEntityWithNoId()
        {
            var entityFromFixture = _fixture.Build<EntityWithNonId>().Create();
            _mongoHelper.Save(entityFromFixture);

            var entitytFromDb = _mongoHelper.FindOne<EntityWithNonId>(entityFromFixture.Name);

            entitytFromDb.Name.ShouldBe(entityFromFixture.Name);
            entitytFromDb.Age.ShouldBe(entityFromFixture.Age);
        }
    }

    public class EntityWithObjectId : MongoEntityWithObjectId
    {
        public override ObjectId Id { get; set; }
        public string String { get; set; }
        public bool Bool { get; set; }
        public List<int> List { get; set; }
    }

    public class EntityWithNonObjectIdId : MongoEntity
    {
        public Guid Id { get; set; }
        public string String { get; set; }
        public bool Bool { get; set; }
        public List<int> List { get; set; }
    }

    public class EntityWithNonId : MongoEntity
    {
        [BsonId]
        public string Name { get; set; }
        public int Age { get; set; }
        public override string GetIdName()
        {
            return "_id";
        }
    }
}
