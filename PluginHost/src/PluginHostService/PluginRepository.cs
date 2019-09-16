using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PluginHostService
{
    public class PluginRepository
    {
        private readonly IMongoDatabase mongoDatabase;

        public PluginRepository(IMongoDatabase mongoDatabase)
        {
            this.mongoDatabase = mongoDatabase;
        }

        public async Task<List<PluginInfo>> GetPluginInfos()
        {
            var pluginInfoCollection = mongoDatabase.GetCollection<PluginInfo>("Plugins");
            return await pluginInfoCollection.AsQueryable().ToListAsync();
        }

        [BsonIgnoreExtraElements]
        public class PluginInfo
        {
            [BsonId]
            [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
            public string Id { get; set; }
            public string Name { get; set; }
            public string PluginType { get; set; }
            public string PluginFilename { get; set; }

        }
    }
}
