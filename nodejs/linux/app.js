const express = require("express");
const bodyParser = require("body-parser");
const { v4: uuidv4 } = require("uuid");
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
  let response = await cosmosLib.getNotes();
  res.json(response);
});

app.get("/notes/:id", async (req, res) => {
  const id = req.params.id;
  let resp = await cosmosLib.readById(id);
  res.status(resp.statusCode);
  res.json(resp.content);
});

app.post("/notes", async (req, res) => {
  let contentText = req.body.content;
  let note = { id: uuidv4(), content: contentText };
  cosmosLib.writeData(note);
  res.json(note);
});

app.delete("/notes/:id", async (req, res) => {
  const id = req.params.id;
  let statusCode = await cosmosLib.deleteNote(id);
  res.status(statusCode);
  res.send("");
});

const port = process.env.PORT || 3000;
try {
  app.listen(port, () => console.log(`Server is listening on port ${port}`));
} catch (err) {
  console.log("Error starting server");
  console.log(err);
}
