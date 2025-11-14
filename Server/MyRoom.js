const { Room } = require("colyseus");
const { Schema, MapSchema, type } = require("@colyseus/schema");

// Player Schema
class Player extends Schema {
  constructor() {
    super();
  }
}

type("number")(Player.prototype, "x");
type("number")(Player.prototype, "y");
type("number")(Player.prototype, "z");

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
    
    // Create new player
    const player = new Player();
    player.x = 0;
    player.y = 0;
    player.z = 0;
    
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