const { Server } = require("colyseus");
const { createServer } = require("http");
const express = require("express");
const { monitor } = require("@colyseus/monitor");

const app = express();
app.use(express.json());

const gameServer = new Server({
  server: createServer(app)
});

// Monitor panel
app.use("/colyseus", monitor());

// Import your room
const { MyRoom } = require("./MyRoom");

// Define your room
gameServer.define("my_room", MyRoom);

gameServer.listen(2567);
console.log("Server started on http://localhost:2567");