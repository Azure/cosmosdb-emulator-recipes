const express = require("express");
const bodyParser = require("body-parser");
const ObjectId = require("mongodb").ObjectId;
const cosmosLib = require("./cosmoslib");
var morgan = require("morgan");
cosmosLib.init();

const app = express();
app.use(morgan("combined"));
app.use(bodyParser.json());

app.get("/", function (req, res) {
  res.send("OK");
});

app.get("/notes", async (req, res) => {
  try {
    let response = await cosmosLib.getNotes();
    res.json(response);
  }
  catch (err) {
    res.status(500).send({ error: err.message })
  }
});

app.get("/notes/:id", async (req, res) => {
  try {
    const id = req.params.id;
    let resp = await cosmosLib.readById(id);
    res.status(resp.statusCode);
    res.json(resp.content);
  }
  catch (err) {
    res.status(500).send({ error: err.message })
  }
});

app.post("/notes", async (req, res) => {
  try {
    let contentText = req.body.content;
    let note = { _id: new ObjectId(), content: contentText };
    await cosmosLib.writeNote(note);
    res.json(note);
  }
  catch (err) {
    res.status(500).send({ error: err.message })
  }
});

app.delete("/notes/:id", async (req, res) => {
  try {
    const id = req.params.id;
    let resp = await cosmosLib.deleteNote(id);
    res.status(resp.statusCode).send("");
  }
  catch (err) {
    res.status(500).send({ error: err.message })
  }
});

const port = process.env.PORT || 3000;
try {
  app.listen(port, () => console.log(`Server is listening on port ${port}`));
} catch (err) {
  console.log("Error starting server");
  console.log(err);
}
