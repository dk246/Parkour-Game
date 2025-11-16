const { Room } = require("colyseus");
const { Schema, MapSchema, type } = require("@colyseus/schema");

// Player Schema
class Player extends Schema {
  constructor() {
    super();
    this.x = 0;
    this.y = 0;
    this.z = 0;
    this.name = "Player"; // ✅ Initialize default value
  }
}

// ✅ DEFINE ALL TYPES (including name)
type("number")(Player.prototype, "x");
type("number")(Player.prototype, "y");
type("number")(Player.prototype, "z");
type("string")(Player.prototype, "name"); // ✅ CRITICAL: Add this line

// Room State
class MyRoomState extends Schema {
  constructor() {
    super();
    this.players = new MapSchema();
  }
}

type({ map: Player })(MyRoomState.prototype, "players");

// Room Logic
class MyRoom extends Room {
  onCreate(options) {
    this.setState(new MyRoomState());
    console.log("Room created!");

    // Handle player movement
    this.onMessage("updatePosition", (client, message) => {
      const player = this.state.players.get(client.sessionId);
      if (player) {
        player.x = message.x;
        player.y = message.y;
        player.z = message.z;
      }
    });
  }

  onJoin(client, options) {
    console.log(client.sessionId, "joined!");
    console.log("Received options:", options);
    console.log("Player name:", options.name);

    // Create new player
    const player = new Player();
    player.x = 0;
    player.y = 0;
    player.z = 0;
    player.name = options.name || "Player";

    console.log("Set player.name to:", player.name);

    this.state.players.set(client.sessionId, player);
  }

  onLeave(client, consented) {
    console.log(client.sessionId, "left!");
    this.state.players.delete(client.sessionId);
  }

  onDispose() {
    console.log("Room disposed!");
  }
}

exports.MyRoom = MyRoom;
