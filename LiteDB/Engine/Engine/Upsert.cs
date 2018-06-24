﻿using System;
using System.Collections.Generic;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Implement upsert command to documents in a collection. Calls update on all documents,
        /// then any documents not updated are then attempted to insert.
        /// This will have the side effect of throwing if duplicate items are attempted to be inserted. Returns true if document is inserted
        /// </summary>
        public bool Upsert(string collection, BsonDocument doc, BsonAutoId autoId = BsonAutoId.ObjectId)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            return this.Upsert(collection, new BsonDocument[] { doc }, autoId) == 1;
        }

        /// <summary>
        /// Implement upsert command to documents in a collection. Calls update on all documents,
        /// then any documents not updated are then attempted to insert.
        /// This will have the side effect of throwing if duplicate items are attempted to be inserted.
        /// </summary>
        public int Upsert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (docs == null) throw new ArgumentNullException(nameof(docs));

            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(SnapshotMode.Write, collection, true);
                var indexer = new IndexService(snapshot);
                var data = new DataService(snapshot);
                var count = 0;
                
                foreach (var doc in docs)
                {
                    var col = snapshot.CollectionPage;

                    // first try update document (if exists _id), if not found, do insert
                    if (doc["_id"] == BsonValue.Null || this.UpdateDocument(snapshot, col, doc, indexer, data) == false)
                    {
                        this.InsertDocument(snapshot, col, doc, autoId, indexer, data);
                        count++;
                    }
                
                    transaction.Safepoint();
                }
                
                // returns how many document was inserted
                return count;
            });
        }
    }
}