const _ = require("lodash");
const CosmosClient = require("@azure/cosmos").CosmosClient;

const endpoint = process.env.COSMOS_ENDPOINT;
const key = process.env.COSMOS_KEY;

let databaseId = process.env.COSMOS_DATABASE || "notes";
const client = new CosmosClient({ endpoint, key });

async function init() {
  try {
    const { database } = await client.databases.createIfNotExists({
      id: databaseId,
    });
    const { container } = await database.containers.createIfNotExists({
      id: databaseId,
    });
  } catch (err) {
    console.log(err);
  }
}

async function getNotes() {
  const { database } = await client.databases.createIfNotExists({
    id: databaseId,
  });
  const { container } = await database.containers.createIfNotExists({
    id: databaseId,
  });
  const { resources } = await container.items.readAll().fetchAll();

  let resp = _.map(resources, _.partialRight(_.pick, ["id", "content"]));
  return resp;
}

async function writeData(note) {
  const { database } = await client.databases.createIfNotExists({
    id: databaseId,
  });
  const { container } = await database.containers.createIfNotExists({
    id: databaseId,
  });
  const itemDef = {
    id: note.id,
    content: note.content,
  };
  await container.items.create(itemDef);
  console.log(`Created item with id: ${itemDef.id}`);
}

async function deleteNote(id) {
  const { database } = await client.databases.createIfNotExists({
    id: databaseId,
  });
  const { container } = await database.containers.createIfNotExists({
    id: databaseId,
  });
  try {
    const { statusCode } = await container.item(id).delete();
    return statusCode;
  } catch (err) {
    console.log(err);
    return err.code;
  }
}

async function readById(id) {
  const { database } = await client.databases.createIfNotExists({
    id: databaseId,
  });
  const { container } = await database.containers.createIfNotExists({
    id: databaseId,
  });

  const item = container.item(id, undefined);

  const { resource: readDoc, statusCode, activityId } = await item.read();
  console.log("statuscode:" + statusCode);
  console.log("activity id:" + activityId);
  if (statusCode == 200) {
    return { statusCode: statusCode, content: readDoc.content };
  } else {
    return { statusCode: 404 };
  }
}

module.exports = { init, getNotes, writeData, deleteNote, readById };
