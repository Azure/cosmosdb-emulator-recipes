const _ = require("lodash");
const MongoClient = require("mongodb").MongoClient;
const ObjectId = require("mongodb").ObjectId;

const connectionString = process.env.COSMOSDB_CONNECTION_STRING;
console.log(connectionString);
let databaseId = process.env.COSMOS_DATABASE || "notes";
const client = new MongoClient(connectionString, {
  tlsAllowInvalidCertificates: true,
});

async function init() {
  try {
    await client.connect();
    const database = client.db(databaseId);
    const collection = database.collection(databaseId);
  } catch (err) {
    console.log(err);
  }
}

async function getNotes() {
  const database = client.db(databaseId);
  const collection = database.collection(databaseId);
  const resources = await collection.find({}).toArray();

  let resp = _.map(resources, _.partialRight(_.pick, ["_id", "content"]));
  return resp;
}

async function writeNote(note) {

  const database = client.db(databaseId);
  const collection = database.collection(databaseId);
  const itemDef = {
    _id: note._id,
    content: note.content,
  };
  await collection.insertOne(itemDef);
}

async function deleteNote(id) {
  const database = client.db(databaseId);
  const collection = database.collection(databaseId);
  const res = await collection.deleteOne({ _id: new ObjectId(id) });
  if (res.deletedCount === 0) {
    throw new Error("No such note exists");
  }
  else {
    return { statusCode: 202 };
  }
}

async function readById(id) {
  const database = client.db(databaseId);
  const collection = database.collection(databaseId);
  const res = await collection.findOne({ "_id": new ObjectId(id) });
  if (!res) {
    return { statusCode: 404, content: {} }
  }
  else {
    return { statusCode: 200, content: res }
  }
}

module.exports = { init, getNotes, writeNote, deleteNote, readById };
