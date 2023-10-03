using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;

using LiteDB;

// ReSharper disable MemberCanBeProtected.Global
namespace NeatNoter;

public abstract class Repository
{
    private readonly BsonMapper bsonMapper;
    private readonly string connectionString;

    protected Repository(string pluginFolder)
    {
        this.bsonMapper = BsonMapper();
        var dirPath = $"{pluginFolder}\\data";
        Directory.CreateDirectory(dirPath);
        this.connectionString = $"Filename={dirPath}\\data.db;connection=shared";
    }

    public void InsertItem<T>(T item)
        where T : class
    {
        using var db = new LiteDatabase(this.connectionString, this.bsonMapper);
        var collection = db.GetCollection<T>();
        collection.Insert(item);
    }

    public void InsertItems<T>(IEnumerable<T> items)
        where T : class
    {
        using var db = new LiteDatabase(this.connectionString, this.bsonMapper);
        var collection = db.GetCollection<T>();
        var enumerable = items as T[] ?? items.ToArray();
        collection.InsertBulk(enumerable, enumerable.Length);
    }

    public void UpsertItems<T>(IEnumerable<T> items)
        where T : class
    {
        using var db = new LiteDatabase(this.connectionString, this.bsonMapper);
        var collection = db.GetCollection<T>();
        collection.Upsert(items);
    }

    public bool UpdateItem<T>(T item)
        where T : class
    {
        using var db = new LiteDatabase(this.connectionString, this.bsonMapper);
        var collection = db.GetCollection<T>();
        return collection.Update(item);
    }

    public bool DeleteItem<T>(int id)
        where T : class
    {
        using var db = new LiteDatabase(this.connectionString, this.bsonMapper);
        var collection = db.GetCollection<T>();
        return collection.Delete(id);
    }

    public int DeleteItems<T>(Expression<Func<T, bool>> predicate)
        where T : class
    {
        using var db = new LiteDatabase(this.connectionString, this.bsonMapper);
        var collection = db.GetCollection<T>();
        return collection.DeleteMany(predicate);
    }

    public T? GetItem<T>(int id)
        where T : class
    {
        using var db = new LiteDatabase(this.connectionString, this.bsonMapper);
        var collection = db.GetCollection<T>();
        return collection.FindById(id);
    }

    public T? GetItem<T>(Expression<Func<T, bool>> predicate)
        where T : class
    {
        return this.InternalGetItems(predicate).FirstOrDefault();
    }

    public IEnumerable<T> GetItems<T>()
        where T : class
    {
        using var db = new LiteDatabase(this.connectionString, this.bsonMapper);
        var collection = db.GetCollection<T>();
        var result = collection.Find(Query.All());
        return result.AsEnumerable();
    }

    public IEnumerable<T> GetItems<T>(Expression<Func<T, bool>> predicate)
        where T : class
    {
        return this.InternalGetItems(predicate);
    }

    public void RebuildIndex<T>(Expression<Func<T, object>> predicate, bool unique = false)
        where T : class
    {
        using var db = new LiteDatabase(this.connectionString, this.bsonMapper);
        var collection = db.GetCollection<T>();
        collection.EnsureIndex(predicate, unique);
    }

    public void RebuildDatabase()
    {
        using var db = new LiteDatabase(this.connectionString, this.bsonMapper);
        db.Rebuild();
    }

    public int GetVersion()
    {
        using var db = new LiteDatabase(this.connectionString, this.bsonMapper);
        return db.UserVersion;
    }

    public void SetVersion(int version)
    {
        using var db = new LiteDatabase(this.connectionString, this.bsonMapper);
        db.UserVersion = version;
    }

    private static BsonMapper BsonMapper()
    {
        var bsonMapper = new BsonMapper
        {
            EmptyStringToNull = false,
            SerializeNullValues = true,
            EnumAsInteger = true,
        };
        bsonMapper.RegisterType(
            vector4 =>
            {
                var doc = new BsonArray
                {
                    new(vector4.X),
                    new(vector4.Y),
                    new(vector4.Z),
                    new(vector4.W),
                };
                return doc;
            },
            (doc) => new Vector4((float)doc[0].AsDouble, (float)doc[1].AsDouble, (float)doc[2].AsDouble, (float)doc[3].AsDouble));
        bsonMapper.RegisterType(
            vector3 =>
            {
                var doc = new BsonArray
                {
                    new(vector3.X),
                    new(vector3.Y),
                    new(vector3.Z),
                };
                return doc;
            },
            (doc) => new Vector3((float)doc[0].AsDouble, (float)doc[1].AsDouble, (float)doc[2].AsDouble));
        return bsonMapper;
    }

    private IEnumerable<T> InternalGetItems<T>(
        Expression<Func<T, bool>>? predicate)
        where T : class
    {
        using var db = new LiteDatabase(this.connectionString, this.bsonMapper);
        var collection = db.GetCollection<T>();

        var result = predicate != null
                         ? collection.Find(predicate)
                         : collection.Find(Query.All());

        return result.AsEnumerable();
    }
}
